﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CrossInform.FrequencyTextAnalyzer.Interfaces;

namespace CrossInform.FrequencyTextAnalyzer
{
    public static class TextUtils
    {
        public const int PROGRESS_BAR_CHARS_COUNT = 30;

        /// <summary>
        /// Синхронный однопоточный метод поиска n-последовательности символов в тексте
        /// </summary>
        /// <param name="text">Входной текст</param>
        /// <param name="charCount">Кол-во символов для поиска</param>
        /// <param name="analyser">Ссылка на объект ITextAnalyser (для проверки запрошена ли отмена). Использование null допускается</param>
        /// <returns></returns>
        public static Dictionary<string, int> FindCharSequenceInText(string text, int charCount, ITextAnalyser analyser)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            char[] buffer = new char[charCount];
            int curentCharsCount = 0;
            int index = 0;

            while (index < text.Length)
            {
                // HACK: Искуственная задержка
                //Thread.Sleep(200);

                if (analyser != null)
                {
                    if (analyser.IsAbortRequested)
                        break;
                }

                char ch = text[index];
                if (char.IsLetter(ch))
                {
                    buffer[curentCharsCount] = char.ToLower(ch);
                    curentCharsCount++;
                }
                else curentCharsCount = 0;
            
                // Набралось нужное кол-во символов. Можно добавить его в словарь.
                if (curentCharsCount == charCount)
                {
                    // TODO: метод с инстанцированием новой строки каждый раз при проверке не самый эфективный... возможно стоит использовать хэшсет...
                    string bufferString = new string(buffer);
                    if (result.ContainsKey(bufferString))
                    {
                        result[bufferString] = result[bufferString] + 1;
                    }
                    else
                    {
                        result.Add(bufferString, 1);
                    }

                    // Сдвиг символов в буфере к концу массива (первый элемент выдавливается в конец)
                    char temp = buffer[curentCharsCount - 1];
                    int shiftPointer = 0;
                    while(shiftPointer < curentCharsCount - 1)
                    {
                        buffer[shiftPointer] = buffer[shiftPointer + 1];
                        buffer[shiftPointer + 1] = temp;
                        shiftPointer++;
                    }
                    curentCharsCount--;
                }
            
                index++;
            }

            return result;
        }

        public static string FormatTextAnalyseResult(ITextStatisticsAnalyseResult result, int samplesCount = 10)
        {
            // Убеждаюсь что кол-во результатов выборки не превышает общее кол-во результатов
            samplesCount = result.StatisticsResult.Count < samplesCount ? result.StatisticsResult.Count : samplesCount;

            // Использую StringBuilder т.к. предпологается частая модификация стринга (особенно при большом выборке). Каждое изменение
            // стринга вызывает создание нового экземпляра и аллокацию памяти под него

            StringBuilder sb = new StringBuilder();
            sb.Append("Общее кол-во символов: ");
            sb.Append(result.GetOriginText().GetText().Length);
            sb.Append("\n");
            sb.Append("Кол-во уникальных последовательностей символов: ");
            sb.Append(result.StatisticsResult.Count);
            sb.Append("\n");
            sb.Append("Самые часто встречаемые последовательности символов:\n\n");
            
            // Сортировка коллекции по убыванию и выборка первых n элементов для вывода
            // Использование LINQа самое простое решение... но не самое быстрое...
            var resultCollection = (from i in result.StatisticsResult orderby i.Value descending select i).Take(samplesCount);

            int index = 1;
            foreach (var item in resultCollection)
            {
                sb.Append(index);
                sb.Append(" - '");
                sb.Append(item.Key);
                sb.Append("': ");
                sb.Append(item.Value);
                sb.Append("\n");
                index++;
            }
            
            sb.Append("\n");
            sb.Append("Затраченное время: ");
            sb.Append(result.GetExecutionDuration());
            sb.Append(" мс\n");
            return sb.ToString();
        }
    }
}
