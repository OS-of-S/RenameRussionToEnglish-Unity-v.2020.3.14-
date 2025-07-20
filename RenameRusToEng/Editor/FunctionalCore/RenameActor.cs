using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.SceneManagement;

namespace RenameRusToEng
{

    /// <summary>
    /// Функциональное ядро Перептолмачивателя (непосредственно алгоритмы поиска и замены).
    /// </summary>
    class RenameActor
    {

        /// <summary>
        /// Для замены кириллицы простым транслитом.
        /// </summary>
        static readonly Dictionary<char, string> TranslitDict = new Dictionary<char, string>()
    {
        {'а', "a" },
        {'б', "b" },
        {'в', "v" },
        {'г', "g" },
        {'д', "d" },
        {'е', "e" },
        {'ё', "yo" },
        {'ж', "zh" },
        {'з', "z" },
        {'и', "i" },
        {'й', "j" },
        {'к', "k" },
        {'л', "l" },
        {'м', "m" },
        {'н', "n" },
        {'о', "o" },
        {'п', "p" },
        {'р', "r" },
        {'с', "s" },
        {'т', "t" },
        {'у', "u" },
        {'ф', "f" },
        {'х', "h" },
        {'ц', "c" },
        {'ч', "ch" },
        {'ш', "sh" },
        {'щ', "shch" },
        {'ъ', "" },
        {'ы', "y" },
        {'ь', "" },
        {'э', "e" },
        {'ю', "yu" },
        {'я', "ya" }
    };

        const string NativeCaps = @"ЁА-Я";
        const string NativeSmall = @"ёа-я";
        const string NativeAlphabet = NativeCaps + NativeSmall; // Регулярное выражение для любого символа исходного языка, от которого мы бы хотели избавиться в проекте. (ЁёА-я)
        const string NativeWord = "[" + NativeAlphabet + "]+"; // Для поиска последовательностей русских символов (не обязательно отдельные слова). Альтернатива: "("+NativePascalWord+")+".
        const string NativePascalWord = @"(?n)(?<=([^ЁА-Я]|\G))[ЁА-Я]?[ёа-я]+|[ЁА-Я]+(?-n)"; // Для разделения последовательностей русских символов на отдельные слова, если наименование оформлено в PascalCase или просто разделено по регистру. Например - "гусятницаНомерОДИН" будет разделено на "гусятница", "Номер" и "ОДИН".
        const string ForeignPascalWord = @"(?n)(?<=([^A-Z]|\G))[A-Z]?[a-z]+|[A-Z]+(?-n)";
        const string PascalWord = NativePascalWord + "|" + ForeignPascalWord;
        Regex NativeWordRegex = new Regex(NativeWord, RegexOptions.Compiled);
        Regex NativePascalWordRegex = new Regex(NativePascalWord, RegexOptions.Compiled);
        Regex ForeignPascalWordRegex = new Regex(ForeignPascalWord, RegexOptions.Compiled);
        Regex PascalWordRegex = new Regex(PascalWord, RegexOptions.Compiled);
        Regex AutoFindRegex; // Регулярное выражение для автоматичекого поиска русизмов (зависит от настроек).
        Regex BigNativeLetterBRegex = new Regex(@"\G[" + NativeCaps + "]", RegexOptions.Compiled);
        Regex SmallNativeLetterBRegex = new Regex(@"\G[" + NativeSmall + "]", RegexOptions.Compiled);
        Regex BigNativeLetterERegex = new Regex("[" + NativeCaps + @"]\G", RegexOptions.RightToLeft | RegexOptions.Compiled);
        Regex SmallNativeLetterERegex = new Regex("[" + NativeSmall + @"]\G", RegexOptions.RightToLeft | RegexOptions.Compiled);
        Regex NativeLetterBRegex = new Regex(@"\G[" + NativeAlphabet + "]", RegexOptions.Compiled);
        Regex NativeLetterERegex = new Regex("[" + NativeAlphabet + @"]\G", RegexOptions.RightToLeft | RegexOptions.Compiled);
        public RenameSettingsWindow root_settings_window;
        public bool make_changes; // Если false, то программа не будет вносить реальных изменений в файлы проекта, а только найдёт русизмы.

        public RenameSettingsWindow.RenameSettings current_settings; // Настройки, при которых запущен процесс перетолмачивания (копируются с менюшки).

        List<PreparedDictElement> ProvidedRazgovornik = new List<PreparedDictElement>();
        public AutoSortedDict AutoRazgovornik = new AutoSortedDict();
        Dictionary<string, DictElement> AutoFoundWords = new Dictionary<string, DictElement>(); // Словарь для быстрой проверки наличия слов в списке.


        public List<ObjectSubstitutionInfo> ProvidedSubstitutions; // Список предусмотренных замен (зафиксированы пользователем в Разговорнике).
        public List<ObjectSubstitutionInfo> AdditionSubstitutions; // Список непредусмотренных замен (обнаруженные последовательности русских символов, не покрытые Разговорником).


        /// <summary>
        /// Функция, дополняющая незавершённый отчёт о заменах более полным контекстом замены и корректирующая номер символа начала вхождения в тексте, если тот был изменён.
        /// </summary>
        List<LogResults> PrepareOneStringLogs(List<LastMatchesInfo> MatchesLogInfos, string input, string output)
        {
            List<LogResults> Substitutions = new List<LogResults>();
            int changed_char_index = 0;
            for (int i = 0; i < MatchesLogInfos.Count; i++)
            {
                Match match = MatchesLogInfos[i].match;
                string replacement = MatchesLogInfos[i].replacement;

                if (i > 0)
                {
                    Match prev_match = MatchesLogInfos[i - 1].match;
                    changed_char_index += match.Index - (prev_match.Index + prev_match.Length);
                }
                else changed_char_index = match.Index;

                int line;
                if (make_changes) line = changed_char_index;
                else line = match.Index;

                LogResults logRsults = new LogResults(MatchesLogInfos[i].dict_element, match, input, output, replacement, changed_char_index, line, current_settings.logsContextSize);

                Substitutions.Add(logRsults);
                changed_char_index += replacement.Length;
            }

            return Substitutions;
        }


        /// <summary>
        /// Простой транслит.
        /// </summary>
        ReplaceResults translit_text(string input)
        {
            PreparedDictElement _nowProcessedDictEl = null;
            List<LastMatchesInfo> _nowProcessedMatches = new List<LastMatchesInfo>();

            string output;
            MatchCollection matches;
            matches = AutoFindRegex.Matches(input);

            if (matches.Count > 0)
            {
                output = "";
                int section_start = 0;

                foreach (Match match in matches)
                {
                    output += input.Substring(section_start, match.Index - section_start);
                    string replacement = "";
                    for (int i = match.Index; i < match.Index + match.Length; i++)
                    {
                        string next_letter = TranslitDict[char.ToLower(input[i])];
                        if (char.IsUpper(input[i])) next_letter = char.ToUpper(next_letter[0]) + next_letter.Substring(1, next_letter.Length - 1);
                        replacement += next_letter;
                    }

                    section_start = match.Index + match.Length;

                    output += replacement;
                    _nowProcessedMatches.Add(new LastMatchesInfo(_nowProcessedDictEl, match, replacement));
                }

                Match lastMatch = matches[matches.Count - 1];
                int substring_begin = lastMatch.Index + lastMatch.Length;
                output += input.Substring(substring_begin, input.Length - substring_begin);
            }
            else output = input;

            // Результат:
            ReplaceResults results = new ReplaceResults();
            results.output = output;
            results.Logs = PrepareOneStringLogs(_nowProcessedMatches, input, output);
            return results;
        }

        /// <summary>
        /// Автоматическое сопостовление регистров совпадения и замены (например, если поиск замен не зависит от регистра, ведь нам желательно сохранить формат).
        /// </summary>
        string MaintainLettersCase(string original, string replacement)
        {
            if (current_settings.CaseMaintainType == 0) return replacement;


            string output = "";
            int section_start = 0;
            MatchCollection PascalsOriginal = PascalWordRegex.Matches(original);
            MatchCollection PascalsReplacement = PascalWordRegex.Matches(replacement);
            if (PascalsOriginal.Count == 0) return replacement; // На случай если не с чем сравнить.

            string MaintainLettersCaseSimple(string _original, string _replacement)
            {
                // ВЫЯСНЯЕМ ТИП ОФОРМЛЕНИЯ СЛОВА _original:
                int type; // 0 = все буквы маленькие, 1 = первая буква большая, 2 = все буквы большие, 3 = другое
                bool allUpper = true;
                bool allLower = true;
                for (int i = _original.Length - 1; i > 0; i--)
                    if (char.IsLower(_original[i])) allUpper = false;
                    else allLower = false;

                if (allLower && char.IsLower(_original[0])) type = 0;
                else
                {
                    if (char.IsUpper(_original[0]))
                    {
                        type = 3;
                        if (allLower) type = 1;
                        if (allUpper) type = 2;
                    }
                    else type = 3;
                }

                // МЕНЯЕМ _replacement В СООТВЕТСТВИИ С ТИПОМ:
                switch (type)
                {
                    case 0:
                        return _replacement.ToLower();
                    case 1:
                        return char.ToUpper(_replacement[0]) + _replacement.Substring(1).ToLower();
                    case 2:
                        return _replacement.ToUpper();
                    default:
                        return _replacement;
                }
            }

            for (int i = 0; i < PascalsReplacement.Count; i++)
            {
                Match match = PascalsReplacement[i];
                output += replacement.Substring(section_start, match.Index - section_start);
                int original_i;
                switch (current_settings.CaseMaintainType)
                {
                    case RenameSettingsWindow.CaseMaintainEnum.PASCALCASE_LAST_STABLE:
                        original_i = Math.Min(i, PascalsOriginal.Count - 1); // Если заменяемая последовательность делится при помощи PascalCase на большее количество слов чем в оригинальной строке, то последние слова заменяемой последовательности форматируются в соответствии с форматом последнего слова из оригинала.
                        break;
                    case RenameSettingsWindow.CaseMaintainEnum.PASCALCASE_ROUND:
                        original_i = i % PascalsOriginal.Count; // Если заменяемая последовательность делится при помощи PascalCase на большее количество слов чем в оригинальной строке, то последние слова заменяемой последовательности форматируются в соответствии с первыми словами из оригинала по кругу.
                        break;
                    default:
                        original_i = Math.Min(i, PascalsOriginal.Count - 1);
                        break;
                }
                output += MaintainLettersCaseSimple(PascalsOriginal[original_i].Value, match.Value);
            }

            if (PascalsReplacement.Count > 0)
            {
                Match lastMatch = PascalsReplacement[PascalsReplacement.Count - 1];
                int substring_begin = lastMatch.Index + lastMatch.Length;
                output += replacement.Substring(substring_begin, replacement.Length - substring_begin);
            }
            else output = replacement;

            return output;
        }


        /// <summary>
        /// Вспомогательная функция (производит замену со сбором информации об оных во внешних списках; используется для формирования MatchEvaluator в обрабатывающих текст функциях).
        /// </summary>
        string CollectMatchesAndReplace(Match match, List<LastMatchesInfo> _nowProcessedMatches, PreparedDictElement _nowProcessedDictEl)
        {
            string replace = match.Result(_nowProcessedDictEl.Regex_eng);
            if (!current_settings.LetterCase) replace = MaintainLettersCase(match.Value, replace);
            LastMatchesInfo match_info = new LastMatchesInfo(_nowProcessedDictEl, match, replace);
            _nowProcessedMatches.Add(match_info);
            return replace;
        }

        /// <summary>
        /// Формирует MatchEvaluator, производящий замены по Разговорнику со сбором информации об оных.
        /// </summary>
        /// <param name="_nowProcessedMatches">Список, в который будет собираться информация о найденных совпадениях (обязан быть "локальным" для каждой использующей его функции, чтобы код был чётким и последовательным, а ещё так будет потенциал для распараллеливания).</param>
        /// <param name="_nowProcessedDictEl">Элемент Разговорника (введённый пользователем или сформированный алгоритмом автоматически), соответствующий обрабатываемой замене.</param>
        MatchEvaluator MakeProcessMatchesEvaulator(List<LastMatchesInfo> _nowProcessedMatches, PreparedDictElement _nowProcessedDictEl)
        {
            return new MatchEvaluator((Match match) => CollectMatchesAndReplace(match, _nowProcessedMatches, _nowProcessedDictEl));
        }


        ReplaceResults ReplaceWithRazgovornik(string input)
        {
            string output = input;
            List<LogResults> CurrentLogs = new List<LogResults>();

            foreach (PreparedDictElement RElement in ProvidedRazgovornik)
            {
                //Debug.Log(RElement.original_rus+ "     " + RElement.original_eng);
                List<LastMatchesInfo> _nowProcessedMatches = new List<LastMatchesInfo>();
                MatchEvaluator evaluator = MakeProcessMatchesEvaulator(_nowProcessedMatches, RElement); //В этом эвалуаторе пополняется NowProcessedMatches.
                output = RElement.Regex_rus.Replace(input, evaluator);

                CurrentLogs.AddRange(PrepareOneStringLogs(_nowProcessedMatches, input, output));

                input = output;
            }
            ReplaceResults results = new ReplaceResults();
            results.output = output;
            results.Logs = CurrentLogs;

            return results;
        }


        /// <summary>
        /// Согласно выбранному режиму обрабатывает конкретное вхождение match автоматически найденного русизма и сохраняет информацию о проделанных действиях.
        /// </summary>
        string CollectAdditionalWordsAndReplace(Match match, List<LastMatchesInfo> _nowProcessedMatches)
        {
            string rus = match.Value;
            if (!current_settings.LetterCase) rus = rus.ToLower();
            DictElement CoresspondedWord;

            if (AutoFoundWords.ContainsKey(rus))
            {
                CoresspondedWord = AutoFoundWords[rus];
            }
            else
            {
                string eng = "";
                switch (current_settings.user_type)
                {
                    case RenameSettingsWindow.UserType.RAZGOVORNIK: // Просто Разговорник (без доп-замен, но с поиском слов).
                        eng = match.Value; // Дублирую слово в колонку разговорника, на случай если пользователь не захочет его заменять.
                        break;
                        //case RenameSettingsWindow.UserType.RAZGOVORNIK_TRANSLATE: // Разговорник + переводчик
                        //eng = Получить_из_переводчика(match.Value);
                        //eng = MaintainLettersCase(match.Value, eng);
                        //eng = eng.Replace(" ", ""); // Так как в любом случае в найденной оригинальной строке не было пробелов, а автопереводчик мог их добавить.
                        //if (ReplaceDashes) eng = eng.Replace("-", "_");
                        //break;
                }

                CoresspondedWord = new DictElement(rus, eng);
                AutoFoundWords.Add(rus, CoresspondedWord);
                AutoRazgovornik.Add(CoresspondedWord);
            }

            string replace;
            if (current_settings.user_type == RenameSettingsWindow.UserType.RAZGOVORNIK)
            {
                replace = match.Value;
            }
            else
            {
                replace = CoresspondedWord.eng;
                if (!current_settings.LetterCase) replace = MaintainLettersCase(match.Value, replace);
            }

            LastMatchesInfo match_info = new LastMatchesInfo(null, match, replace);
            _nowProcessedMatches.Add(match_info);
            return replace;
        }

        /// <summary>
        /// Формирует MatchEvaluator, производящий замены (при необходимости!) и сбор информации об оных автоматически найденных вхождений.
        /// </summary>
        /// /// <param name="_nowProcessedMatches">Список, в который будет собираться информация о найденных совпадениях (обязан быть "локальным" для каждой использующей его функции, чтобы код был чётким и последовательным, а ещё так будет потенциал для распараллеливания).</param>
        MatchEvaluator MakeProcessAdditionalWordsEvaulator(List<LastMatchesInfo> _nowProcessedMatches)
        {
            return new MatchEvaluator((Match match) => CollectAdditionalWordsAndReplace(match, _nowProcessedMatches));
        }


        /// <summary>
        /// Автоматически собирает русизмы в тексте и заменяет их, согласно выбранному режиму. (Второй этап работы Перептолмачивателя.)
        /// </summary>
        ReplaceResults ReplaceWithWords(string input)
        {

            string output;
            List<LastMatchesInfo> _nowProcessedMatches = new List<LastMatchesInfo>();

            MatchEvaluator evaluator = MakeProcessAdditionalWordsEvaulator(_nowProcessedMatches); //В этом эвалуаторе пополняется NowProcessedMatches.

            output = AutoFindRegex.Replace(input, evaluator);

            List<LogResults> CurrentLogs = PrepareOneStringLogs(_nowProcessedMatches, input, output);
            ReplaceResults results = new ReplaceResults();
            results.output = output;
            results.Logs = CurrentLogs;

            return results;
        }


        /// <summary>
        /// Получает на вход текст. Обрабатывает его согласно настройкам Перептолмачивателя. Возвращает результат с отчётом о проделанных действиях.
        /// </summary>
        ReplaceResults process_text(string text)
        {
            ReplaceResults results = new ReplaceResults();
            results.Logs = new List<LogResults>();
            results.AdditionLogs = new List<LogResults>();

            ReplaceResults partitial_res;

            if (current_settings.user_type == RenameSettingsWindow.UserType.TRANSLIT)
            {
                partitial_res = translit_text(text);
                results.Logs.AddRange(partitial_res.Logs);
                text = partitial_res.output;
            }
            else // Режимы, связанные с Разговорником
            {
                // Алгоритм следующий:
                // 1) Пробегается пользовательский Разговорник.
                // 2) Ищутся не указанные в Разговорнике слова, которые обрабатываются соответственно режиму.

                partitial_res = ReplaceWithRazgovornik(text);
                results.Logs.AddRange(partitial_res.Logs);
                text = partitial_res.output;


                if (current_settings.user_type == RenameSettingsWindow.UserType.RAZGOVORNIK_TRANSLIT)
                {
                    partitial_res = translit_text(text);
                    results.AdditionLogs.AddRange(partitial_res.Logs);
                    text = partitial_res.output;
                }
                else
                {
                    partitial_res = ReplaceWithWords(text); // Остальные режимы внутри.
                    results.AdditionLogs.AddRange(partitial_res.Logs);
                    text = partitial_res.output;
                }
            }

            results.output = text;
            return results;
        }

        /// <summary>
        /// Вспомогательный класс для рекурсивного сбора данных об обработанных объектах (файлах, папках, GameObject'ах, и т.д.)
        /// </summary>
        private class ObjectsSubstitutionResults
        {
            public List<ObjectSubstitutionInfo> main_info = new List<ObjectSubstitutionInfo>(); // Соответствует ProvidedSubstitutions.
            public List<ObjectSubstitutionInfo> addition_info = new List<ObjectSubstitutionInfo>(); // Соответствует AdditionSubstitution.
        }


        /// <summary>
        /// Обрабатывает файл скрипта. Возвращает результат с отчётом о проделанных действиях.
        /// </summary>
        ReplaceResults process_script(MonoScript script_to_process)
        {
            ReplaceResults result = new ReplaceResults();
            result.Logs = new List<LogResults>();
            result.AdditionLogs = new List<LogResults>();
            string path = AssetDatabase.GetAssetPath(script_to_process);

            IEnumerable<string> strings_to_process;
            if (current_settings.separate_strings_text)
                strings_to_process = File.ReadLines(path);
            else
                strings_to_process = new string[] { File.ReadAllText(path) };

            int line_counter = 0;
            List<string> output_lines = new List<string>();
            foreach (string line in strings_to_process)
            {
                ReplaceResults scr_substitutions = process_text(line);

                output_lines.Add(scr_substitutions.output);
                foreach (var log in scr_substitutions.Logs) log.lineNumber = line_counter;
                foreach (var log in scr_substitutions.AdditionLogs) log.lineNumber = line_counter;
                result.Logs.AddRange(scr_substitutions.Logs);
                result.AdditionLogs.AddRange(scr_substitutions.AdditionLogs);
                line_counter++;
            }
            result.output = string.Join("\n", output_lines);


            if (make_changes && (result.Logs.Count > 0 || result.AdditionLogs.Count > 0))
            {
                File.WriteAllText(path, result.output); //.WriteAllLines?
            }

            return result;
        }


        /// <summary>
        /// Обрабатывает GameObject'ы. Возвращает результат с отчётом о проделанных действиях.
        /// </summary>
        ObjectsSubstitutionResults process_gameobjects(GameObject[] objects_to_process)
        {
            ObjectsSubstitutionResults result = new ObjectsSubstitutionResults();

            foreach (GameObject obj in objects_to_process)
            {
                ObjectSubstitutionInfo obj_main_info = new ObjectSubstitutionInfo(obj, ObjectSubstitutionInfo.ObjectType.GAMEOBJECT);
                ObjectSubstitutionInfo obj_addition_info = new ObjectSubstitutionInfo(obj, ObjectSubstitutionInfo.ObjectType.GAMEOBJECT);
                obj_main_info.displayed_text = obj.name;
                obj_addition_info.displayed_text = obj.name;

                //Если префаб: (обрабатываем до обработки имени, т.к. иначе имя обнуляется)
                if (current_settings.recursive_prefs && PrefabUtility.IsAnyPrefabInstanceRoot(obj))//obj.hideFlags == HideFlags.HideInHierarchy)
                {
                    GameObject pref_obj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj);
                    ObjectsSubstitutionResults pref_info = process_prefab(pref_obj);
                    if (pref_info.main_info.Count > 0)
                    {
                        ObjectSubstitutionInfo pref_root = new ObjectSubstitutionInfo(pref_obj, 0);
                        pref_root.displayed_text = "Исходный префаб";
                        pref_root.Childrens = pref_info.main_info;
                        obj_main_info.Childrens.Add(pref_root);
                    }

                    if (pref_info.addition_info.Count > 0)
                    {
                        ObjectSubstitutionInfo pref_root = new ObjectSubstitutionInfo(pref_obj, 0);
                        pref_root.displayed_text = "Исходный префаб";
                        pref_root.Childrens = pref_info.addition_info;
                        obj_addition_info.Childrens.Add(pref_root);
                    }
                }

                //Обработка имени:
                ReplaceResults new_name_info = process_text(obj.name);
                obj_main_info.Details.AddRange(new_name_info.Logs);
                obj_addition_info.Details.AddRange(new_name_info.AdditionLogs);

                if (current_settings.ReplaceDashes) new_name_info.output = new_name_info.output.Replace("-", "_");
                if (current_settings.ReplaceSpaces) new_name_info.output = new_name_info.output.Replace(" ", "_");

                if (new_name_info.Logs.Count > 0) obj_main_info.displayed_text += " -> " + new_name_info.output;
                if (new_name_info.AdditionLogs.Count > 0) obj_addition_info.displayed_text += " -> " + new_name_info.output;


                if (make_changes)
                {
                    obj.name = new_name_info.output;
                }

                //Дети:
                GameObject[] childrens = new GameObject[obj.transform.childCount];
                for (int i = 0; i < childrens.Length; i++) childrens[i] = obj.transform.GetChild(i).gameObject;
                ObjectsSubstitutionResults childrens_info = process_gameobjects(childrens);
                obj_main_info.Childrens.AddRange(childrens_info.main_info);
                obj_addition_info.Childrens.AddRange(childrens_info.addition_info);

                //Добавление (или не добавление) в результат:
                if (obj_main_info.Details.Count > 0 || obj_main_info.Childrens.Count > 0)
                    result.main_info.Add(obj_main_info);
                if (obj_addition_info.Details.Count > 0 || obj_addition_info.Childrens.Count > 0)
                    result.addition_info.Add(obj_addition_info);
            }

            return result;
        }


        /// <summary>
        /// Обрабатывает игровую сцену. Возвращает результат с отчётом о проделанных действиях.
        /// </summary>
        ObjectsSubstitutionResults process_scene(SceneAsset scene_to_process)
        {
            string path = AssetDatabase.GetAssetPath(scene_to_process);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            ObjectsSubstitutionResults result = process_gameobjects(scene.GetRootGameObjects());

            if (make_changes)
            {
                bool Success = EditorSceneManager.SaveScene(scene, path);
                if (!Success)
                    Debug.LogError("Не удалось сохранить изменения в сцене " + path);
            }

            EditorSceneManager.CloseScene(scene, true);
            return result;

        }

        /// <summary>
        /// Обрабатывает префаб. Возвращает результат с отчётом о проделанных действиях.
        /// </summary>
        ObjectsSubstitutionResults process_prefab(GameObject prefab_to_process)
        {
            string path = AssetDatabase.GetAssetPath(prefab_to_process);
            var root_gameobject = PrefabUtility.LoadPrefabContents(path);

            GameObject[] childrens = new GameObject[root_gameobject.transform.childCount];
            // Обрабатываем сразу детей по двум причинам: имя корневого объекта в любом случае неизменимо и является именем файла префаба, и 2) мы должны избежать зацикленности при рекурсивной обработке префабов.
            for (int i = 0; i < childrens.Length; i++) childrens[i] = root_gameobject.transform.GetChild(i).gameObject;
            ObjectsSubstitutionResults result = process_gameobjects(childrens);

            if (make_changes)
            {
                bool Success;
                PrefabUtility.SaveAsPrefabAsset(root_gameobject, path, out Success);
                if (!Success)
                    Debug.LogError("Не удалось сохранить изменения в префабе " + path);
            }

            PrefabUtility.UnloadPrefabContents(root_gameobject);
            return result;

        }

        /// <summary>
        ///  Возвращает все ассеты внутри папки (файлы и папки, учтённые в AssetDatabase) без рекурисвого просмотра подпапок.
        /// </summary>
        List<UnityEngine.Object> GetFolderContents(string folder_path)
        {
            List<UnityEngine.Object> Childrens = new List<UnityEngine.Object>();
            foreach (string raw_child_path in Directory.GetFileSystemEntries(folder_path)) // На форумах пишут, что это может сломаться, если в названии ассетов встретится слово Assets (но альтернативы гораздо хуже...)
            {
                string child_path = raw_child_path.Replace("\\", "/");
                var child_obj = AssetDatabase.LoadAssetAtPath(child_path, AssetDatabase.GetMainAssetTypeAtPath(child_path));
                if (child_obj != null) Childrens.Add(child_obj); // Не уверен, учитывает ли это все исключения, но файлы .meta считываются как null
            }
            return Childrens;
        }


        /// <summary>
        /// Обрабатывает ассеты. Возвращает результат с отчётом о проделанных действиях.
        /// </summary>
        ObjectsSubstitutionResults process_assets(List<UnityEngine.Object> assets_to_process)
        {

            ObjectsSubstitutionResults result = new ObjectsSubstitutionResults();

            foreach (UnityEngine.Object asset in assets_to_process)
            {
                ObjectSubstitutionInfo current_obj_main_info = new ObjectSubstitutionInfo(asset, 0);
                ObjectSubstitutionInfo current_obj_addition_info = new ObjectSubstitutionInfo(asset, 0);

                string asset_path = AssetDatabase.GetAssetPath(asset);
                string asset_name = Path.GetFileNameWithoutExtension(asset_path);
                string asset_name_with_extension = Path.GetFileName(asset_path);
                string extension = Path.GetExtension(asset_path);
                string new_name = "";

                if (current_settings.names_flag) // Обработка имени:
                {
                    ReplaceResults NameProcessResults = process_text(asset_name);
                    new_name = NameProcessResults.output;

                    if (current_settings.ReplaceDashes) NameProcessResults.output = NameProcessResults.output.Replace("-", "_");
                    if (current_settings.ReplaceSpaces) NameProcessResults.output = NameProcessResults.output.Replace(" ", "_");

                    if (make_changes)
                    {
                        AssetDatabase.RenameAsset(asset_path, new_name);
                    }
                    current_obj_main_info.Details = NameProcessResults.Logs;
                    current_obj_addition_info.Details = NameProcessResults.AdditionLogs;
                }

                // Обрабатываем чилдренов:
                if (AssetDatabase.IsValidFolder(asset_path)) // Дети папки
                {
                    ObjectsSubstitutionResults folder_info = process_assets(GetFolderContents(asset_path));

                    if (folder_info.main_info.Count > 0) current_obj_main_info.Childrens.AddRange(folder_info.main_info);
                    if (folder_info.addition_info.Count > 0) current_obj_addition_info.Childrens.AddRange(folder_info.addition_info);
                }
                else
                {
                    Type asset_type = asset.GetType();
                    if (current_settings.code_flag && asset_type == typeof(MonoScript)) // "Дети" скрипта
                    {
                        ObjectSubstitutionInfo make_text_obj_info(List<LogResults> Details)
                        {
                            ObjectSubstitutionInfo _info = new ObjectSubstitutionInfo();
                            _info.displayed_text = "Внутрифайловый текст";
                            _info.Details.AddRange(Details);
                            return _info;
                        }

                        ReplaceResults script_info = process_script((MonoScript)asset);
                        if (script_info.Logs.Count > 0)
                            current_obj_main_info.Childrens.Add(make_text_obj_info(script_info.Logs));
                        if (script_info.AdditionLogs.Count > 0)
                            current_obj_addition_info.Childrens.Add(make_text_obj_info(script_info.AdditionLogs));
                    }

                    if (current_settings.gameobjects_flag)
                    {
                        if (asset_type == typeof(SceneAsset)) // "Дети" сцены
                        {
                            ObjectsSubstitutionResults scene_results = process_scene((SceneAsset)asset);
                            current_obj_main_info.Childrens.AddRange(scene_results.main_info);
                            current_obj_addition_info.Childrens.AddRange(scene_results.addition_info);
                        }

                        if (asset_type == typeof(GameObject)) // "Дети" префаба.
                        {
                            ObjectsSubstitutionResults scene_results = process_prefab((GameObject)asset);
                            current_obj_main_info.Childrens.AddRange(scene_results.main_info);
                            current_obj_addition_info.Childrens.AddRange(scene_results.addition_info);
                        }
                    }
                }


                // Задаём label, который будет отображаться в окне результатов (если есть изменения в имени, то имя отображается жирным текстом.)
                void format_file_label(ObjectSubstitutionInfo obj)
                {
                    obj.displayed_text = asset_name_with_extension;
                    if (obj.Details.Count > 0) // Если есть потенциальные изменения в имени.
                        obj.displayed_text += " -> " + new_name + extension;
                }
                format_file_label(current_obj_main_info);
                format_file_label(current_obj_addition_info);



                // Добавляем в список, если найдены соответствующие последовательности в объекте или в его детях:
                if ((current_obj_main_info.Details.Count > 0) || (current_obj_main_info.Childrens.Count > 0))
                {
                    result.main_info.Add(current_obj_main_info);
                }
                if ((current_obj_addition_info.Details.Count > 0) || (current_obj_addition_info.Childrens.Count > 0))
                {
                    result.addition_info.Add(current_obj_addition_info);
                }

            }
            //Debug.Log("Обрабатываем ассеты: " + result.main_info.Count + " " + result.addition_info.Count);

            return result;
        }

        /// <summary>
        /// Отправная точка выполнения алгоритма.
        /// </summary>
        ObjectsSubstitutionResults StartProcess()
        {
            ObjectsSubstitutionResults results = process_assets(root_settings_window.selected);
            ProvidedSubstitutions = results.main_info;
            AdditionSubstitutions = results.addition_info;
            Debug.Log("ПРОЦЕСС ПЕРЕТОЛМАЧИВАТЕЛЯ ЗАВЕРШОН.");
            return results;
        }

        /// <summary>
        /// Открывает окно с результатами проделанных действий.
        /// </summary>
        void ShowResults()
        {
            Debug.Log("Открываю окно с результатами перептолмачивания...");
            //Debug.Log("Русизмы которые найдены: " + AdditionSubstitutions[0].displayed_text);
            ResultsMainWindow window = EditorWindow.CreateWindow<ResultsMainWindow>("Результаты", desiredDockNextTo: new Type[]
            {
            root_settings_window.GetType()
            });// new ResultsMainWindow(this);
            window.SetData(this);
            window.Show();
        }

        public RenameActor(RenameSettingsWindow root, bool _make_changes = false)
        {
            root_settings_window = root;
            make_changes = _make_changes;
            current_settings = root.settings.Clone(); // копируем настройки.

            AutoSortedDict Razgovornik = root.Razgovornik;
            AutoSortedDict.SortType current_SortRazgovornik = Razgovornik.SortRazgovornik;

            switch (current_settings.AutoFindWordsType)
            {
                case RenameSettingsWindow.AutoFindWordsEnum.PASCAL_CASE_RUS:
                    AutoFindRegex = NativePascalWordRegex;
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.WORD_RUS:
                    AutoFindRegex = NativeWordRegex;
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.ONE_SEPARATOR_RUS: // Один разделяющий символ
                    AutoFindRegex = new Regex(NativeWord + @"(?n)(." + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.TWO_SEPARATOR_RUS: // Два разделяющих символа
                    AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,2}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.THREE_SEPARATOR_RUS: // Три разделяющих символа
                    AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,3}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.FOUR_SEPARATOR_RUS: // Четыре разделяющих символа
                    AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,4}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.FIVE_SEPARATOR_RUS: // Пять разделяющих символов
                    AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,5}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.SENTENCE_RUS: // Предложение
                    AutoFindRegex = new Regex("[А-ЯЁ][а-яё\\s,;:—\\-\\\"]*([;]|[.?!]{0,3}|(?=[A-Za-z]|$))", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.OTHER: // Другое
                    AutoFindRegex = new Regex(current_settings.CustomFindRegex);
                    break;
            }

            if (current_settings.ActuallySortByLength)
            {
                Razgovornik.SortRazgovornik = AutoSortedDict.SortType.BY_LENGHT; // Сортирую по длине. Причины:
                                                                                 //     1) На случай если кому-то понадобится заменять наименования вроде "весёлыйгусь", то можно было бы разрешить вхождения подслов, и при этом всё равно избежать неправильных замен.
                                                                                 //     2) Чтобы учесть и вторую колонку в том числе, для однозначности и определённости, чтобы алгоритм не брал случайное из значений, если ключи в левой колонке Разговорника по недосмотру пользователя совпадут.
            }

            // Подготовка регулярных выражений по элементам Разговорника:
            foreach (DictElement i in Razgovornik)
            {
                string rus = i.rus;
                string eng = i.eng;
                RegexOptions regexOptions = RegexOptions.Compiled; // Чтобы не компилировать их каждое сравнение.

                /*
                if (!current_settings.LetterCase)
                {
                    regexOptions = regexOptions | RegexOptions.IgnoreCase; Так нельзя, потому что есть ещё всякие разные правила. Эту настройку нужно включать выключать в середине шаблона.
                }
                */

                if (!current_settings.UseRegularExpr)
                {
                    rus = Regex.Escape(rus); // Экранируем все символы, которые могут быть по-особому проинтерпретированны регулярным выражением.

                    eng = eng.Replace("$", "$$"); // Экранирую символы $, чтобы регулярка не пыталась их заменить (а замена может произойти в случае подстрок
                                                  // вида "$2" см. документацию о substitutions. Это обрабатывается именно здесь, а не внутри CollectMatchesAndReplace(),
                                                  // чтобы код был консистентным (не сильно зависел от того, используются ли регулярные выражения пользователем или нет),
                                                  // понятным и поддающимся изменениям.
                }

                if (current_settings.ReplaceDashes) eng = eng.Replace("-", "_");
                if (current_settings.ReplaceSpaces) eng = eng.Replace(" ", "_");

                // Если PascalCase и отключен учёт регистра, проще просто обработать этот конфликтный случай отдельно:
                if (!current_settings.LetterCase && !current_settings.UseRegularExpr && !current_settings.SubwordsInside && current_settings.AutoFindWordsType == 0)
                {
                    List<string> ruses_to_process = new List<string>();


                    if (NativeLetterBRegex.Match(rus).Success) // Если первый символ = буква.
                    {
                        string substr = rus.Substring(1);
                        rus = (@"(?n)(((?<=^|[^" + NativeCaps + @"])" + char.ToUpper(rus[0]) + ")|(" + @"(?<=^|([" + NativeCaps + "][" + NativeCaps + "])|[^" + NativeAlphabet + @"])" + char.ToLower(rus[0]) + @"))(?i)" + substr);
                    }
                    else rus = @"(?n)(?i)" + rus;

                    if (NativeLetterERegex.Match(rus).Success) // Если последний символ = буква.
                    {
                        int last_indx = rus.Length - 1;
                        string substr = rus.Substring(0, last_indx);
                        rus = substr + @"(?-i)((" + char.ToUpper(rus[last_indx]) + @"(?=$|[^" + NativeCaps + "])" + ")|(" + char.ToLower(rus[last_indx]) + @"(?=$|[^" + NativeSmall + "])" + "))(?-n)";
                    }
                    else rus = rus + @"(?-i)(?-n)";

                    PreparedDictElement newDictElement = new PreparedDictElement();
                    newDictElement.original_rus = i.rus;
                    newDictElement.original_eng = i.eng;
                    newDictElement.Regex_rus = new Regex(rus, regexOptions);

                    newDictElement.Regex_eng = eng;
                    ProvidedRazgovornik.Add(newDictElement);

                }
                else
                {
                    if (!current_settings.LetterCase)
                    {
                        rus = @"(?i)" + rus + @"(?-i)";
                    }

                    if (!current_settings.UseRegularExpr && !current_settings.SubwordsInside)
                    {
                        if (current_settings.AutoFindWordsType == 0) // "Если нужно учитывать PascalCase..."
                        {
                            if (BigNativeLetterBRegex.Match(i.rus).Success) // "Если первая буква большая..."
                                rus = @"(?<=^|[^" + NativeCaps + @"])" + rus; // "То перед ней не должно быть больших."
                            else
                            {
                                if (SmallNativeLetterBRegex.Match(i.rus).Success) // "Если первая буква маленькая..."
                                    rus = @"(?<=^|([" + NativeCaps + "][" + NativeCaps + "])|[^" + NativeAlphabet + @"])" + rus; // "То перед ней не должно быть маленьких или одной большой."
                            }

                            if (BigNativeLetterERegex.Match(i.rus).Success) // "Если последняя буква большая..."
                                rus = rus + @"(?=$|[^" + NativeCaps + "])"; // "То после неё не должно быть больших."
                            else
                            {
                                if (SmallNativeLetterERegex.Match(i.rus).Success) // "Если последняя буква маленькая..."
                                    rus = rus + @"(?=$|[^" + NativeSmall + "])"; // "То после неё не должно быть маленькой."
                            }
                        }
                        else
                        {
                            if (NativeLetterBRegex.Match(i.rus).Success)
                                rus = @"(?<=^|[^" + NativeAlphabet + @"])" + rus;

                            if (NativeLetterERegex.Match(i.rus).Success)
                                rus = rus + @"(?=$|[^" + NativeAlphabet + "])";
                        }
                    }

                    PreparedDictElement newDictElement = new PreparedDictElement();
                    newDictElement.original_rus = i.rus;
                    newDictElement.original_eng = i.eng;
                    newDictElement.Regex_rus = new Regex(rus, regexOptions);

                    newDictElement.Regex_eng = eng;
                    ProvidedRazgovornik.Add(newDictElement);
                }
            }

            Razgovornik.SortRazgovornik = current_SortRazgovornik; // Восстанавливаем порядок Разговорника.

            StartProcess();
            ShowResults();
        }
    }
}