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
    /// �������������� ���� ������������������ (��������������� ��������� ������ � ������).
    /// </summary>
    class RenameActor
    {

        /// <summary>
        /// ��� ������ ��������� ������� ����������.
        /// </summary>
        static readonly Dictionary<char, string> TranslitDict = new Dictionary<char, string>()
    {
        {'�', "a" },
        {'�', "b" },
        {'�', "v" },
        {'�', "g" },
        {'�', "d" },
        {'�', "e" },
        {'�', "yo" },
        {'�', "zh" },
        {'�', "z" },
        {'�', "i" },
        {'�', "j" },
        {'�', "k" },
        {'�', "l" },
        {'�', "m" },
        {'�', "n" },
        {'�', "o" },
        {'�', "p" },
        {'�', "r" },
        {'�', "s" },
        {'�', "t" },
        {'�', "u" },
        {'�', "f" },
        {'�', "h" },
        {'�', "c" },
        {'�', "ch" },
        {'�', "sh" },
        {'�', "shch" },
        {'�', "" },
        {'�', "y" },
        {'�', "" },
        {'�', "e" },
        {'�', "yu" },
        {'�', "ya" }
    };

        const string NativeCaps = @"��-�";
        const string NativeSmall = @"��-�";
        const string NativeAlphabet = NativeCaps + NativeSmall; // ���������� ��������� ��� ������ ������� ��������� �����, �� �������� �� �� ������ ���������� � �������. (���-�)
        const string NativeWord = "[" + NativeAlphabet + "]+"; // ��� ������ ������������������� ������� �������� (�� ����������� ��������� �����). ������������: "("+NativePascalWord+")+".
        const string NativePascalWord = @"(?n)(?<=([^��-�]|\G))[��-�]?[��-�]+|[��-�]+(?-n)"; // ��� ���������� ������������������� ������� �������� �� ��������� �����, ���� ������������ ��������� � PascalCase ��� ������ ��������� �� ��������. �������� - "������������������" ����� ��������� �� "���������", "�����" � "����".
        const string ForeignPascalWord = @"(?n)(?<=([^A-Z]|\G))[A-Z]?[a-z]+|[A-Z]+(?-n)";
        const string PascalWord = NativePascalWord + "|" + ForeignPascalWord;
        Regex NativeWordRegex = new Regex(NativeWord, RegexOptions.Compiled);
        Regex NativePascalWordRegex = new Regex(NativePascalWord, RegexOptions.Compiled);
        Regex ForeignPascalWordRegex = new Regex(ForeignPascalWord, RegexOptions.Compiled);
        Regex PascalWordRegex = new Regex(PascalWord, RegexOptions.Compiled);
        Regex AutoFindRegex; // ���������� ��������� ��� �������������� ������ �������� (������� �� ��������).
        Regex BigNativeLetterBRegex = new Regex(@"\G[" + NativeCaps + "]", RegexOptions.Compiled);
        Regex SmallNativeLetterBRegex = new Regex(@"\G[" + NativeSmall + "]", RegexOptions.Compiled);
        Regex BigNativeLetterERegex = new Regex("[" + NativeCaps + @"]\G", RegexOptions.RightToLeft | RegexOptions.Compiled);
        Regex SmallNativeLetterERegex = new Regex("[" + NativeSmall + @"]\G", RegexOptions.RightToLeft | RegexOptions.Compiled);
        Regex NativeLetterBRegex = new Regex(@"\G[" + NativeAlphabet + "]", RegexOptions.Compiled);
        Regex NativeLetterERegex = new Regex("[" + NativeAlphabet + @"]\G", RegexOptions.RightToLeft | RegexOptions.Compiled);
        public RenameSettingsWindow root_settings_window;
        public bool make_changes; // ���� false, �� ��������� �� ����� ������� �������� ��������� � ����� �������, � ������ ����� �������.

        public RenameSettingsWindow.RenameSettings current_settings; // ���������, ��� ������� ������� ������� ���������������� (���������� � �������).

        List<PreparedDictElement> ProvidedRazgovornik = new List<PreparedDictElement>();
        public AutoSortedDict AutoRazgovornik = new AutoSortedDict();
        Dictionary<string, DictElement> AutoFoundWords = new Dictionary<string, DictElement>(); // ������� ��� ������� �������� ������� ���� � ������.


        public List<ObjectSubstitutionInfo> ProvidedSubstitutions; // ������ ��������������� ����� (������������� ������������� � ������������).
        public List<ObjectSubstitutionInfo> AdditionSubstitutions; // ������ ����������������� ����� (������������ ������������������ ������� ��������, �� �������� �������������).


        /// <summary>
        /// �������, ����������� ������������� ����� � ������� ����� ������ ���������� ������ � �������������� ����� ������� ������ ��������� � ������, ���� ��� ��� ������.
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
        /// ������� ��������.
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

            // ���������:
            ReplaceResults results = new ReplaceResults();
            results.output = output;
            results.Logs = PrepareOneStringLogs(_nowProcessedMatches, input, output);
            return results;
        }

        /// <summary>
        /// �������������� ������������� ��������� ���������� � ������ (��������, ���� ����� ����� �� ������� �� ��������, ���� ��� ���������� ��������� ������).
        /// </summary>
        string MaintainLettersCase(string original, string replacement)
        {
            if (current_settings.CaseMaintainType == 0) return replacement;


            string output = "";
            int section_start = 0;
            MatchCollection PascalsOriginal = PascalWordRegex.Matches(original);
            MatchCollection PascalsReplacement = PascalWordRegex.Matches(replacement);
            if (PascalsOriginal.Count == 0) return replacement; // �� ������ ���� �� � ��� ��������.

            string MaintainLettersCaseSimple(string _original, string _replacement)
            {
                // �������� ��� ���������� ����� _original:
                int type; // 0 = ��� ����� ���������, 1 = ������ ����� �������, 2 = ��� ����� �������, 3 = ������
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

                // ������ _replacement � ������������ � �����:
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
                        original_i = Math.Min(i, PascalsOriginal.Count - 1); // ���� ���������� ������������������ ������� ��� ������ PascalCase �� ������� ���������� ���� ��� � ������������ ������, �� ��������� ����� ���������� ������������������ ������������� � ������������ � �������� ���������� ����� �� ���������.
                        break;
                    case RenameSettingsWindow.CaseMaintainEnum.PASCALCASE_ROUND:
                        original_i = i % PascalsOriginal.Count; // ���� ���������� ������������������ ������� ��� ������ PascalCase �� ������� ���������� ���� ��� � ������������ ������, �� ��������� ����� ���������� ������������������ ������������� � ������������ � ������� ������� �� ��������� �� �����.
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
        /// ��������������� ������� (���������� ������ �� ������ ���������� �� ���� �� ������� �������; ������������ ��� ������������ MatchEvaluator � �������������� ����� ��������).
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
        /// ��������� MatchEvaluator, ������������ ������ �� ������������ �� ������ ���������� �� ����.
        /// </summary>
        /// <param name="_nowProcessedMatches">������, � ������� ����� ���������� ���������� � ��������� ����������� (������ ���� "���������" ��� ������ ������������ ��� �������, ����� ��� ��� ������ � ����������������, � ��� ��� ����� ��������� ��� �����������������).</param>
        /// <param name="_nowProcessedDictEl">������� ������������ (�������� ������������� ��� �������������� ���������� �������������), ��������������� �������������� ������.</param>
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
                MatchEvaluator evaluator = MakeProcessMatchesEvaulator(_nowProcessedMatches, RElement); //� ���� ���������� ����������� NowProcessedMatches.
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
        /// �������� ���������� ������ ������������ ���������� ��������� match ������������� ���������� ������� � ��������� ���������� � ����������� ���������.
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
                    case RenameSettingsWindow.UserType.RAZGOVORNIK: // ������ ����������� (��� ���-�����, �� � ������� ����).
                        eng = match.Value; // �������� ����� � ������� ������������, �� ������ ���� ������������ �� ������� ��� ��������.
                        break;
                        //case RenameSettingsWindow.UserType.RAZGOVORNIK_TRANSLATE: // ����������� + ����������
                        //eng = ��������_��_�����������(match.Value);
                        //eng = MaintainLettersCase(match.Value, eng);
                        //eng = eng.Replace(" ", ""); // ��� ��� � ����� ������ � ��������� ������������ ������ �� ���� ��������, � �������������� ��� �� ��������.
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
        /// ��������� MatchEvaluator, ������������ ������ (��� �������������!) � ���� ���������� �� ���� ������������� ��������� ���������.
        /// </summary>
        /// /// <param name="_nowProcessedMatches">������, � ������� ����� ���������� ���������� � ��������� ����������� (������ ���� "���������" ��� ������ ������������ ��� �������, ����� ��� ��� ������ � ����������������, � ��� ��� ����� ��������� ��� �����������������).</param>
        MatchEvaluator MakeProcessAdditionalWordsEvaulator(List<LastMatchesInfo> _nowProcessedMatches)
        {
            return new MatchEvaluator((Match match) => CollectAdditionalWordsAndReplace(match, _nowProcessedMatches));
        }


        /// <summary>
        /// ������������� �������� ������� � ������ � �������� ��, �������� ���������� ������. (������ ���� ������ ������������������.)
        /// </summary>
        ReplaceResults ReplaceWithWords(string input)
        {

            string output;
            List<LastMatchesInfo> _nowProcessedMatches = new List<LastMatchesInfo>();

            MatchEvaluator evaluator = MakeProcessAdditionalWordsEvaulator(_nowProcessedMatches); //� ���� ���������� ����������� NowProcessedMatches.

            output = AutoFindRegex.Replace(input, evaluator);

            List<LogResults> CurrentLogs = PrepareOneStringLogs(_nowProcessedMatches, input, output);
            ReplaceResults results = new ReplaceResults();
            results.output = output;
            results.Logs = CurrentLogs;

            return results;
        }


        /// <summary>
        /// �������� �� ���� �����. ������������ ��� �������� ���������� ������������������. ���������� ��������� � ������� � ����������� ���������.
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
            else // ������, ��������� � �������������
            {
                // �������� ���������:
                // 1) ����������� ���������������� �����������.
                // 2) ������ �� ��������� � ������������ �����, ������� �������������� �������������� ������.

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
                    partitial_res = ReplaceWithWords(text); // ��������� ������ ������.
                    results.AdditionLogs.AddRange(partitial_res.Logs);
                    text = partitial_res.output;
                }
            }

            results.output = text;
            return results;
        }

        /// <summary>
        /// ��������������� ����� ��� ������������ ����� ������ �� ������������ �������� (������, ������, GameObject'��, � �.�.)
        /// </summary>
        private class ObjectsSubstitutionResults
        {
            public List<ObjectSubstitutionInfo> main_info = new List<ObjectSubstitutionInfo>(); // ������������� ProvidedSubstitutions.
            public List<ObjectSubstitutionInfo> addition_info = new List<ObjectSubstitutionInfo>(); // ������������� AdditionSubstitution.
        }


        /// <summary>
        /// ������������ ���� �������. ���������� ��������� � ������� � ����������� ���������.
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
        /// ������������ GameObject'�. ���������� ��������� � ������� � ����������� ���������.
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

                //���� ������: (������������ �� ��������� �����, �.�. ����� ��� ����������)
                if (current_settings.recursive_prefs && PrefabUtility.IsAnyPrefabInstanceRoot(obj))//obj.hideFlags == HideFlags.HideInHierarchy)
                {
                    GameObject pref_obj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj);
                    ObjectsSubstitutionResults pref_info = process_prefab(pref_obj);
                    if (pref_info.main_info.Count > 0)
                    {
                        ObjectSubstitutionInfo pref_root = new ObjectSubstitutionInfo(pref_obj, 0);
                        pref_root.displayed_text = "�������� ������";
                        pref_root.Childrens = pref_info.main_info;
                        obj_main_info.Childrens.Add(pref_root);
                    }

                    if (pref_info.addition_info.Count > 0)
                    {
                        ObjectSubstitutionInfo pref_root = new ObjectSubstitutionInfo(pref_obj, 0);
                        pref_root.displayed_text = "�������� ������";
                        pref_root.Childrens = pref_info.addition_info;
                        obj_addition_info.Childrens.Add(pref_root);
                    }
                }

                //��������� �����:
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

                //����:
                GameObject[] childrens = new GameObject[obj.transform.childCount];
                for (int i = 0; i < childrens.Length; i++) childrens[i] = obj.transform.GetChild(i).gameObject;
                ObjectsSubstitutionResults childrens_info = process_gameobjects(childrens);
                obj_main_info.Childrens.AddRange(childrens_info.main_info);
                obj_addition_info.Childrens.AddRange(childrens_info.addition_info);

                //���������� (��� �� ����������) � ���������:
                if (obj_main_info.Details.Count > 0 || obj_main_info.Childrens.Count > 0)
                    result.main_info.Add(obj_main_info);
                if (obj_addition_info.Details.Count > 0 || obj_addition_info.Childrens.Count > 0)
                    result.addition_info.Add(obj_addition_info);
            }

            return result;
        }


        /// <summary>
        /// ������������ ������� �����. ���������� ��������� � ������� � ����������� ���������.
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
                    Debug.LogError("�� ������� ��������� ��������� � ����� " + path);
            }

            EditorSceneManager.CloseScene(scene, true);
            return result;

        }

        /// <summary>
        /// ������������ ������. ���������� ��������� � ������� � ����������� ���������.
        /// </summary>
        ObjectsSubstitutionResults process_prefab(GameObject prefab_to_process)
        {
            string path = AssetDatabase.GetAssetPath(prefab_to_process);
            var root_gameobject = PrefabUtility.LoadPrefabContents(path);

            GameObject[] childrens = new GameObject[root_gameobject.transform.childCount];
            // ������������ ����� ����� �� ���� ��������: ��� ��������� ������� � ����� ������ ���������� � �������� ������ ����� �������, � 2) �� ������ �������� ������������� ��� ����������� ��������� ��������.
            for (int i = 0; i < childrens.Length; i++) childrens[i] = root_gameobject.transform.GetChild(i).gameObject;
            ObjectsSubstitutionResults result = process_gameobjects(childrens);

            if (make_changes)
            {
                bool Success;
                PrefabUtility.SaveAsPrefabAsset(root_gameobject, path, out Success);
                if (!Success)
                    Debug.LogError("�� ������� ��������� ��������� � ������� " + path);
            }

            PrefabUtility.UnloadPrefabContents(root_gameobject);
            return result;

        }

        /// <summary>
        ///  ���������� ��� ������ ������ ����� (����� � �����, ������� � AssetDatabase) ��� ����������� ��������� ��������.
        /// </summary>
        List<UnityEngine.Object> GetFolderContents(string folder_path)
        {
            List<UnityEngine.Object> Childrens = new List<UnityEngine.Object>();
            foreach (string raw_child_path in Directory.GetFileSystemEntries(folder_path)) // �� ������� �����, ��� ��� ����� ���������, ���� � �������� ������� ���������� ����� Assets (�� ������������ ������� ����...)
            {
                string child_path = raw_child_path.Replace("\\", "/");
                var child_obj = AssetDatabase.LoadAssetAtPath(child_path, AssetDatabase.GetMainAssetTypeAtPath(child_path));
                if (child_obj != null) Childrens.Add(child_obj); // �� ������, ��������� �� ��� ��� ����������, �� ����� .meta ����������� ��� null
            }
            return Childrens;
        }


        /// <summary>
        /// ������������ ������. ���������� ��������� � ������� � ����������� ���������.
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

                if (current_settings.names_flag) // ��������� �����:
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

                // ������������ ���������:
                if (AssetDatabase.IsValidFolder(asset_path)) // ���� �����
                {
                    ObjectsSubstitutionResults folder_info = process_assets(GetFolderContents(asset_path));

                    if (folder_info.main_info.Count > 0) current_obj_main_info.Childrens.AddRange(folder_info.main_info);
                    if (folder_info.addition_info.Count > 0) current_obj_addition_info.Childrens.AddRange(folder_info.addition_info);
                }
                else
                {
                    Type asset_type = asset.GetType();
                    if (current_settings.code_flag && asset_type == typeof(MonoScript)) // "����" �������
                    {
                        ObjectSubstitutionInfo make_text_obj_info(List<LogResults> Details)
                        {
                            ObjectSubstitutionInfo _info = new ObjectSubstitutionInfo();
                            _info.displayed_text = "�������������� �����";
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
                        if (asset_type == typeof(SceneAsset)) // "����" �����
                        {
                            ObjectsSubstitutionResults scene_results = process_scene((SceneAsset)asset);
                            current_obj_main_info.Childrens.AddRange(scene_results.main_info);
                            current_obj_addition_info.Childrens.AddRange(scene_results.addition_info);
                        }

                        if (asset_type == typeof(GameObject)) // "����" �������.
                        {
                            ObjectsSubstitutionResults scene_results = process_prefab((GameObject)asset);
                            current_obj_main_info.Childrens.AddRange(scene_results.main_info);
                            current_obj_addition_info.Childrens.AddRange(scene_results.addition_info);
                        }
                    }
                }


                // ����� label, ������� ����� ������������ � ���� ����������� (���� ���� ��������� � �����, �� ��� ������������ ������ �������.)
                void format_file_label(ObjectSubstitutionInfo obj)
                {
                    obj.displayed_text = asset_name_with_extension;
                    if (obj.Details.Count > 0) // ���� ���� ������������� ��������� � �����.
                        obj.displayed_text += " -> " + new_name + extension;
                }
                format_file_label(current_obj_main_info);
                format_file_label(current_obj_addition_info);



                // ��������� � ������, ���� ������� ��������������� ������������������ � ������� ��� � ��� �����:
                if ((current_obj_main_info.Details.Count > 0) || (current_obj_main_info.Childrens.Count > 0))
                {
                    result.main_info.Add(current_obj_main_info);
                }
                if ((current_obj_addition_info.Details.Count > 0) || (current_obj_addition_info.Childrens.Count > 0))
                {
                    result.addition_info.Add(current_obj_addition_info);
                }

            }
            //Debug.Log("������������ ������: " + result.main_info.Count + " " + result.addition_info.Count);

            return result;
        }

        /// <summary>
        /// ��������� ����� ���������� ���������.
        /// </summary>
        ObjectsSubstitutionResults StartProcess()
        {
            ObjectsSubstitutionResults results = process_assets(root_settings_window.selected);
            ProvidedSubstitutions = results.main_info;
            AdditionSubstitutions = results.addition_info;
            Debug.Log("������� ����������������� ��������.");
            return results;
        }

        /// <summary>
        /// ��������� ���� � ������������ ����������� ��������.
        /// </summary>
        void ShowResults()
        {
            Debug.Log("�������� ���� � ������������ �����������������...");
            //Debug.Log("������� ������� �������: " + AdditionSubstitutions[0].displayed_text);
            ResultsMainWindow window = EditorWindow.CreateWindow<ResultsMainWindow>("����������", desiredDockNextTo: new Type[]
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
            current_settings = root.settings.Clone(); // �������� ���������.

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
                case RenameSettingsWindow.AutoFindWordsEnum.ONE_SEPARATOR_RUS: // ���� ����������� ������
                    AutoFindRegex = new Regex(NativeWord + @"(?n)(." + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.TWO_SEPARATOR_RUS: // ��� ����������� �������
                    AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,2}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.THREE_SEPARATOR_RUS: // ��� ����������� �������
                    AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,3}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.FOUR_SEPARATOR_RUS: // ������ ����������� �������
                    AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,4}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.FIVE_SEPARATOR_RUS: // ���� ����������� ��������
                    AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,5}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.SENTENCE_RUS: // �����������
                    AutoFindRegex = new Regex("[�-ߨ][�-��\\s,;:�\\-\\\"]*([;]|[.?!]{0,3}|(?=[A-Za-z]|$))", RegexOptions.Compiled);
                    break;
                case RenameSettingsWindow.AutoFindWordsEnum.OTHER: // ������
                    AutoFindRegex = new Regex(current_settings.CustomFindRegex);
                    break;
            }

            if (current_settings.ActuallySortByLength)
            {
                Razgovornik.SortRazgovornik = AutoSortedDict.SortType.BY_LENGHT; // �������� �� �����. �������:
                                                                                 //     1) �� ������ ���� ����-�� ����������� �������� ������������ ����� "����������", �� ����� ���� �� ��������� ��������� �������, � ��� ���� �� ����� �������� ������������ �����.
                                                                                 //     2) ����� ������ � ������ ������� � ��� �����, ��� ������������� � �������������, ����� �������� �� ���� ��������� �� ��������, ���� ����� � ����� ������� ������������ �� ���������� ������������ ��������.
            }

            // ���������� ���������� ��������� �� ��������� ������������:
            foreach (DictElement i in Razgovornik)
            {
                string rus = i.rus;
                string eng = i.eng;
                RegexOptions regexOptions = RegexOptions.Compiled; // ����� �� ������������� �� ������ ���������.

                /*
                if (!current_settings.LetterCase)
                {
                    regexOptions = regexOptions | RegexOptions.IgnoreCase; ��� ������, ������ ��� ���� ��� ������ ������ �������. ��� ��������� ����� �������� ��������� � �������� �������.
                }
                */

                if (!current_settings.UseRegularExpr)
                {
                    rus = Regex.Escape(rus); // ���������� ��� �������, ������� ����� ���� ��-������� �������������������� ���������� ����������.

                    eng = eng.Replace("$", "$$"); // ��������� ������� $, ����� ��������� �� �������� �� �������� (� ������ ����� ��������� � ������ ��������
                                                  // ���� "$2" ��. ������������ � substitutions. ��� �������������� ������ �����, � �� ������ CollectMatchesAndReplace(),
                                                  // ����� ��� ��� ������������� (�� ������ ������� �� ����, ������������ �� ���������� ��������� ������������� ��� ���),
                                                  // �������� � ����������� ����������.
                }

                if (current_settings.ReplaceDashes) eng = eng.Replace("-", "_");
                if (current_settings.ReplaceSpaces) eng = eng.Replace(" ", "_");

                // ���� PascalCase � �������� ���� ��������, ����� ������ ���������� ���� ����������� ������ ��������:
                if (!current_settings.LetterCase && !current_settings.UseRegularExpr && !current_settings.SubwordsInside && current_settings.AutoFindWordsType == 0)
                {
                    List<string> ruses_to_process = new List<string>();


                    if (NativeLetterBRegex.Match(rus).Success) // ���� ������ ������ = �����.
                    {
                        string substr = rus.Substring(1);
                        rus = (@"(?n)(((?<=^|[^" + NativeCaps + @"])" + char.ToUpper(rus[0]) + ")|(" + @"(?<=^|([" + NativeCaps + "][" + NativeCaps + "])|[^" + NativeAlphabet + @"])" + char.ToLower(rus[0]) + @"))(?i)" + substr);
                    }
                    else rus = @"(?n)(?i)" + rus;

                    if (NativeLetterERegex.Match(rus).Success) // ���� ��������� ������ = �����.
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
                        if (current_settings.AutoFindWordsType == 0) // "���� ����� ��������� PascalCase..."
                        {
                            if (BigNativeLetterBRegex.Match(i.rus).Success) // "���� ������ ����� �������..."
                                rus = @"(?<=^|[^" + NativeCaps + @"])" + rus; // "�� ����� ��� �� ������ ���� �������."
                            else
                            {
                                if (SmallNativeLetterBRegex.Match(i.rus).Success) // "���� ������ ����� ���������..."
                                    rus = @"(?<=^|([" + NativeCaps + "][" + NativeCaps + "])|[^" + NativeAlphabet + @"])" + rus; // "�� ����� ��� �� ������ ���� ��������� ��� ����� �������."
                            }

                            if (BigNativeLetterERegex.Match(i.rus).Success) // "���� ��������� ����� �������..."
                                rus = rus + @"(?=$|[^" + NativeCaps + "])"; // "�� ����� �� �� ������ ���� �������."
                            else
                            {
                                if (SmallNativeLetterERegex.Match(i.rus).Success) // "���� ��������� ����� ���������..."
                                    rus = rus + @"(?=$|[^" + NativeSmall + "])"; // "�� ����� �� �� ������ ���� ���������."
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

            Razgovornik.SortRazgovornik = current_SortRazgovornik; // ��������������� ������� ������������.

            StartProcess();
            ShowResults();
        }
    }
}