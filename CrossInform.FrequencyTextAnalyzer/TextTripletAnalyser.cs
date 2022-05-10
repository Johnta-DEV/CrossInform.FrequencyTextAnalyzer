﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CrossInform.FrequencyTextAnalyzer.Interfaces;

namespace CrossInform.FrequencyTextAnalyzer
{
    // TODO: Данный класс уже имеет функционал для нахождения n-последовательностей символов в тексте.
    // Достаточно лишь изменить имя класса и добавить ввод желаемого кол-ва символов

    /// <summary>
    /// Частотный анализатор текста находящий триплеты в тексте (3 буквы подряд).
    /// </summary>
    public class TextTripletAnalyser : ITextAnalyser
    {
        public const int CHARS_COUNT_TO_SEARCH = 3;

        public const int MIN_THREADS_COUNT = 1;
        public const int MAX_THREADS_COUNT = 48;

        private bool isAbortRequested = false;
        private bool isAnalysing = false;
        private int threadsCount = MAX_THREADS_COUNT;
        private int charsCountToSearch = CHARS_COUNT_TO_SEARCH;

        /// <summary>
        /// Синхронный вариант метода (останавливает родительский поток) анализа текста в многопоточном режиме.
        /// </summary>
        /// <param name="textProvider"></param>
        /// <returns></returns>
        public ITextStatisticsAnalyseResult SyncAnalyseText(ITextProvider textProvider)
        {
            // Этапы
            // 1) Определить кол-во потоков
            // 2) Разделить входной текст на соответствующее кол-во сегментов (с учётом пробелов)
            // 3) Запустить N-потоков которые будут анализировать свои сегменты и сохранять результаты в свои бакеты
            // 4) Подождать пока все потоки завершатся после чего соеденить результаты
            // 5) Вывести статистику по результатам
            
            string text = textProvider.GetText();
            if (String.IsNullOrEmpty(text))
            {
                throw new Exception("Входная строка была пуста или null!");
            }

            // Копирование кол-ва потоков в отдельнюу переменную для защиты от изменения в процессе работы алгоритма

            //int currentThreadsCount = threadsCount;
            // HACK: Хардкод
            int currentThreadsCount = 1;

            isAnalysing = true;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Разделение текста на кол-во сегментов соответствующее кол-ву желаемых потоков
            string[] textSegments = SplitTextOnSegments(text, currentThreadsCount);

            int segmentsCount = textSegments.Length;

            // Создание массива тасков в количестве равном полученному кол-ву сегментов
            Task<Dictionary<char[], int>>[] tasks = new Task<Dictionary<char[], int>>[segmentsCount];
            for (int i = 0; i < segmentsCount; i++)
            {
                tasks[i] = Task<Dictionary<char[], int>>.Factory.StartNew(() => { return TextUtils.FindCharSequenceInText(textSegments[i], charsCountToSearch); });
            }
            
            Dictionary<char[], int>[] taskResults = new Dictionary<char[], int>[segmentsCount];

            // Попытка считать результаты выполнения тасков. Если результат не готов - поток блокируется до его получения
            // Таким образом собираются результаты всех тасков (вместо использования .Join)
            for (int i = 0; i < segmentsCount; i++)
            {
                taskResults[i] = tasks[i].Result;
            }
            
            AnalyseResultState resultState = AnalyseResultState.Complete;
            Dictionary<char[], int> resultDictionary = new Dictionary<char[], int>();

            // Проверка был ли вызван аборт
            if (isAbortRequested)
            {
                resultState = AnalyseResultState.Aborted;
            }
            else
            {
                // Если аборта небыло - объеденение коллекций
                foreach (Dictionary<char[], int> dict in taskResults)
                {
                    var keys = dict.Keys;
                    foreach (var key in keys)
                    {
                        if (resultDictionary.ContainsKey(key))
                        {
                            resultDictionary[key] = resultDictionary[key] + dict[key];
                        }
                        else
                        {
                            resultDictionary.Add(key, dict[key]);
                        }
                    }
                }
            }

            TextAnalyseResult result = new TextAnalyseResult(resultState, sw.ElapsedMilliseconds, resultDictionary, textProvider);
            sw.Stop();
            isAnalysing = false;
            isAbortRequested = false;
            return result;
        }


        // TODO: Вынести этот метод в отдельный Util класс т.к. в каждой реализации интерфейса ITextAnalyser будет необходим подобный класс

        /// <summary>
        /// Разделяет входной текст на сегменты по знаку пробела. Сегментов может быть меньше ожидаемого из-за меньшего
        /// кол-ва пробелов или их отсутствия.
        /// </summary>
        /// <param name="text">Водной текст</param>
        /// <param name="segmentsCount">Кол-во сегментов</param>
        /// <returns></returns>
        private string[] SplitTextOnSegments(string text, int segmentsCount)
        {
            if (segmentsCount == 1)
                return new string[] { text };
            // HACK: хардкод
            else throw new NotImplementedException();
        }
        

        public bool IsAnalysing
        {
            get
            {
                return isAnalysing;
            }
        }

        public int ThreadsCount
        {
            get
            {
                return threadsCount;
            }

            set
            {
                // TODO: решить должен ли здесь вобде быть экспешен, или достаточно задавать max значение...
                if (value < MIN_THREADS_COUNT || value > MAX_THREADS_COUNT)
                    throw new Exception("Заданное число поток вне допустимых границ: " + MAX_THREADS_COUNT + "-" + MAX_THREADS_COUNT + "! Получено значение: " + value);
                this.threadsCount = value;
            }
        }

        public void RequestAbort()
        {
            isAbortRequested = true;
        }
    }
    
}