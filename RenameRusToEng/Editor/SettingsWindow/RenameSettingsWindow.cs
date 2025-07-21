// Умрике от Смекты )
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using SmectUI;

namespace RenameRusToEng
{

    /// <summary>
    /// Окно настроек Перептолмачивателя.
    /// </summary>
    public class RenameSettingsWindow : EditorWindow
    {

        public AutoSortedDict Razgovornik;

        public List<UnityEngine.Object> selected;
        PointerManipulator selectedAssets_dragandrop;
        PointerManipulator razgovornik_dragandrop;

        bool RazgovornikPaddle = false;
        Vector2 scroll_pos = new Vector2(0, 0);
        bool AutoFindWordsPaddle = false;
        bool OtherSettingsPaddle = false;

        static readonly string[] spreadsheet_separator_types = { ",", ";", "|", " ", "\t", ", ", "; " };
        private static readonly string[] spreadsheet_separator_types_guitext = { ",", ";", "|", "Пробел", "Табуляция", ", (с пробелом)", "; (с пробелом)", "Другой" };

        public bool RazgovornikChanged = false;

        AssetsList SelectedVisuals;

        public ListViewAdvanced RazgovornikContainer;
        public const float SpaceBetweenElements = 20;
        const int items_in_list_on_screen = 9;
        static float TextFieldHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        public static float ListsHeight = TextFieldHeight * (items_in_list_on_screen + 0.5f);
        const int ListAdditionSpace_elements = 4; // Удобное пустое пространство в конце списка: сколько элементов ещё можно добавить прежде чем придётся перематывать список ниже.
        static float ListAdditionSpace = ListAdditionSpace_elements * TextFieldHeight;
        const int AdditionSpaceBegining = items_in_list_on_screen - 1; //items_in_list_on_screen - ListAdditionSpace_elements - 2; Закомментил альтернативу, которая просто выглядит менее удачной.
                                                                       //VisualElement root;


        //int sorted_focus = -1;
        /*bool dict_changed = false;
        bool dict_changed_this_frame
        {
            set
            {
                dict_changed = dict_changed || value;
                dict_changed_this_frame = value;
            }
            get { return dict_changed; }
        }*/

        Action DictListsOnGUI(int i)
        {
            return () =>
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                int i2;
                if (i < Razgovornik.Count)
                {
                    string old_rus = Razgovornik[i].rus;
                    string old_eng = Razgovornik[i].eng;
                    i2 = i * 2;
                    string current_rus_name = "dictfield_" + i2;
                    string current_eng_name = "dictfield_" + (i2 + 1);
                    GUI.SetNextControlName(current_rus_name);
                    Razgovornik[i].rus = EditorGUILayout.DelayedTextField(Razgovornik[i].rus);
                    GUI.SetNextControlName(current_eng_name);
                    Razgovornik[i].eng = EditorGUILayout.DelayedTextField(Razgovornik[i].eng);

                    if (old_rus != Razgovornik[i].rus || old_eng != Razgovornik[i].eng)
                    {
                        if (Razgovornik[i].rus == "" && Razgovornik[i].eng == "")
                        {
                            Razgovornik.RemoveAt(i);
                            RazgovornikContainer.Refresh();
                        }
                        else if (SortRazgovornik > 0)
                        {
                            int new_sorted_indx = Razgovornik.ResortElement(i);
                        //scroll_pos = clamp between 0&1((((new_sorted_indx + 0.5) * TextFieldHeight - half_rect_kotori_vidim) / (RazgovornikVisual.Count * text_height + AdditionSpace)) * scroll_height?);
                    }
                    }
                }
                else
                {
                    i2 = Razgovornik.Count * 2;
                    GUI.SetNextControlName("dictfield_" + i2);
                    string newrus = EditorGUILayout.DelayedTextField("");
                    GUI.SetNextControlName("dictfield_" + (i2 + 1));
                    string neweng = EditorGUILayout.DelayedTextField("");
                    if (newrus != "" || neweng != "")
                    {
                        DictElement new_element = new DictElement(newrus, neweng);
                        if (SortRazgovornik > 0)
                        {
                            int new_sorted_indx = Razgovornik.AddSorted(new_element);
                        //scroll_pos = clamp between 0&1((((new_sorted_indx + 0.5) * TextFieldHeight - half_rect_kotori_vidim) / (RazgovornikVisual.Count * text_height + AdditionSpace)) * scroll_height?);
                    }
                        else
                        {
                            Razgovornik.Add(new_element);
                            RazgovornikContainer.Refresh();
                        //просто прибавить к скроллу высоту одного текстополя чтоб сдвинуться вниз?
                    }
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    RazgovornikChanged = true;
                }


                RazgovornikControlls();
            };
        }

        public enum UserType
        {
            TRANSLIT, // Простой транслит
            RAZGOVORNIK, // Разговорник
            RAZGOVORNIK_TRANSLIT, // Разговорник + транслит
                                  //RAZGOVORNIK_TRANSLATE, // Разговорник + переводчик (НЕ ИМПЛЕМЕНТИРОВАННО! Планировалось подключение к автоматическому переводчику.)
        }

        public enum CaseMaintainEnum
        {
            DISABLED, // Отключено
            PASCALCASE_LAST_STABLE, // PascalCase с устойчивым последним регистром
            PASCALCASE_ROUND, // PascalCase со сравнением "по кругу"
        }

        public enum AutoFindWordsEnum
        {
            PASCAL_CASE_RUS, // Русское слово в PascalCase
            WORD_RUS, // Непрерывная последовательность русских символов
            ONE_SEPARATOR_RUS, // 1 разделяющий символ
            TWO_SEPARATOR_RUS, // 2 разделяющих символа
            THREE_SEPARATOR_RUS, // 3 разделяющих символа
            FOUR_SEPARATOR_RUS, // 4 разделяющих символа
            FIVE_SEPARATOR_RUS, // 5 разделяющих символов
            SENTENCE_RUS, // Предложение на русском языке
            OTHER, // Другое
        }

        public enum SaveLoadEnum
        {
            TXT_FILE, // Текстовый файл
            CSV_TABLE, // Таблица
            OTHER, // Другие, неподдерживаемые разрешения
        }

        /// <summary>
        /// Класс сохраняемых и передаваемых настроек Перептолмачивателя.
        /// </summary>
        public class RenameSettings
        {
            private RenameSettingsWindow root = null;
            public UserType user_type;

            public bool gameobjects_flag;
            public bool recursive_prefs;
            public bool names_flag;
            public bool code_flag;

            public AutoSortedDict.SortType _sortRazgovornik;
            public bool UseRegularExpr;
            public bool LetterCase;
            public bool ActuallySortByLength;
            public bool SubwordsInside;

            public AutoFindWordsEnum AutoFindWordsType;
            public string CustomFindRegex;

            public CaseMaintainEnum CaseMaintainType;
            public bool ReplaceDashes;
            public bool ReplaceSpaces;
            public SaveLoadEnum SaveLoadType;
            public string text_separator;
            public int spreadsheet_separator;
            public bool separate_strings_text;
            public int logsContextSize;

            public RenameSettings(RenameSettingsWindow _root)
            {
                root = _root;
            }

            public RenameSettings Clone()
            {
                return (RenameSettings)MemberwiseClone();
            }

            public void SetDefaults()
            {
                user_type = UserType.RAZGOVORNIK;

                gameobjects_flag = true;
                recursive_prefs = true;
                names_flag = true;
                code_flag = false;

                _sortRazgovornik = 0;
                UseRegularExpr = false;
                LetterCase = false;
                ActuallySortByLength = true;
                SubwordsInside = false;

                AutoFindWordsType = 0;
                CustomFindRegex = "[А - ЯЁ][а - яё\\s,;:—\\-\\\"]*([;]|[.?!]{0,3}|(?=[A-Za-z]|$))";

                CaseMaintainType = CaseMaintainEnum.PASCALCASE_LAST_STABLE;
                ReplaceDashes = false;
                ReplaceSpaces = false;
                SaveLoadType = 0; // текстовый файл
                text_separator = " ☚►❤◄☛ ";
                spreadsheet_separator = 0; // ", "
                separate_strings_text = true;
                logsContextSize = 15;
            }
        }

        public RenameSettings settings;

        public AutoSortedDict.SortType SortRazgovornik // локальный управлятор, чтобы и в сохраняемый класс значение внести, и Разговорник отсортировать.
        {
            get => settings._sortRazgovornik;
            set
            {
                settings._sortRazgovornik = value;
                Razgovornik.SortRazgovornik = value;
            }
        }

        private string GetDefaultSavePath([System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
        {
            // Просто на всякий случай, если скрипт будет положен куда-то кроме папки Editor:
            if (sourceFilePath == "") sourceFilePath = Application.dataPath + Path.DirectorySeparatorChar + "RenameRusToEng";
            else sourceFilePath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(sourceFilePath)));
            return sourceFilePath;
        }

        private string GetDefaultSettingsSavePath() => Path.Combine(GetDefaultSavePath(), "RRTE_Settings.json");

        private string GetRazgovornikExtension()
        {
            switch (settings.SaveLoadType)
            {
                case SaveLoadEnum.TXT_FILE: return "txt";
                case SaveLoadEnum.CSV_TABLE: return "csv";
                default: return "txt";
            }
        }

        private string GetDefaultRazgovornikPath() => Path.Combine(GetDefaultSavePath(), "Razgovornik.") + GetRazgovornikExtension();

        public void SaveSettings(string saveFilePath = "")
        {
            if (saveFilePath == "") saveFilePath = GetDefaultSettingsSavePath();
            string stringJSON = JsonUtility.ToJson(settings, true);
            File.WriteAllText(saveFilePath, stringJSON);
        }

        public void LoadToSettings(string saveFilePath = "")
        {
            if (saveFilePath == "") saveFilePath = GetDefaultSettingsSavePath();
            if (File.Exists(saveFilePath))
            {
                string stringJSON = File.ReadAllText(saveFilePath);
                JsonUtility.FromJsonOverwrite(stringJSON, settings);
            }
        }

        SaveLoadEnum RazgovornikAutoSaveType(string saveFilePath)
        {
            switch (Path.GetExtension(saveFilePath))
            {
                case ".txt": return SaveLoadEnum.TXT_FILE;
                case ".csv": return SaveLoadEnum.CSV_TABLE;
                default: return SaveLoadEnum.OTHER; // Сигнализирует об ошибке.
            }
        }

        private string[] GetRazgovornikSep(SaveLoadEnum SaveLoadType_current) // Возвращает массив разделителей, т.к. в общем случае split принимает именно массив, допуская более одного разделителя.
        {
            switch (SaveLoadType_current)
            {
                case SaveLoadEnum.TXT_FILE:
                    return new string[] { settings.text_separator };
                case SaveLoadEnum.CSV_TABLE:
                    if (settings.spreadsheet_separator < spreadsheet_separator_types.Length)
                        return new string[] { spreadsheet_separator_types[settings.spreadsheet_separator] };
                    else return new string[] { settings.text_separator };
                default: return new string[] { settings.text_separator };
            }
        }


        /// <summary>
        /// Вызывает окно с выбором папки и имени для сохраняемого файла Разговорника.
        /// </summary>
        bool SaveRazgovornik_with_window()
        {
            string save_path = EditorUtility.SaveFilePanel("Сохранение Разговорника", GetDefaultSavePath(), "Razgovornik", GetRazgovornikExtension());
            if (save_path != "")
            {
                SaveRazgovornik(save_path);
                return true;
            }
            return false;
        }

        public void SaveRazgovornik(string saveFilePath = "")
        {
            SaveLoadEnum SaveLoadType_current;
            if (saveFilePath == "")
            {
                SaveLoadType_current = settings.SaveLoadType;
                saveFilePath = GetDefaultRazgovornikPath();
            }
            else
            {
                SaveLoadType_current = RazgovornikAutoSaveType(saveFilePath);
                if (SaveLoadType_current == SaveLoadEnum.OTHER)
                {
                    EditorUtility.DisplayDialog("Ошибка!", "Такие расширения файлов Разговорник не поддерживает.", "Ок");
                    return;
                }
                else RazgovornikChanged = false;
            }

            string sep = GetRazgovornikSep(SaveLoadType_current)[0];

            TextWriter tw = new StreamWriter(saveFilePath, false);

            foreach (DictElement i in Razgovornik) tw.WriteLine(i.rus + sep + i.eng);

            tw.Close();
        }

        public void LoadToRazgovornik(bool rewrite = false, string saveFilePath = "")
        {
            SaveLoadEnum SaveLoadType_current;
            if (saveFilePath == "")
            {
                SaveLoadType_current = settings.SaveLoadType;
                saveFilePath = GetDefaultRazgovornikPath();
            }
            else
            {
                SaveLoadType_current = RazgovornikAutoSaveType(saveFilePath);
                if (SaveLoadType_current < 0)
                {
                    EditorUtility.DisplayDialog("Ошибка!", "Такие расширения файлов Разговорник не поддерживает.", "Ок");
                    return;
                }
            }

            //UnityEngine.Object save_file
            if (File.Exists(saveFilePath))
            {
                TextReader tr = new StreamReader(saveFilePath);
                string[] sep = GetRazgovornikSep(SaveLoadType_current);

                // Проверка на правильность оформления файла:
                int errors = 0;
                int str_num = 0;
                while (true)
                {
                    string str = tr.ReadLine();
                    if (str == null) break;
                    int elements_in_string = str.Split(sep, StringSplitOptions.None).Length;
                    if (elements_in_string != 2)
                    {
                        errors++;
                        Debug.LogError("Ошибка в оформлении файла Разговорника: строка " + str_num + ", вместо двух элементов обнаружено " + elements_in_string + ".\nВ качестве разделителя использованы:" + sep + "\n\n" + str + "\n\n" + saveFilePath + "\n\n");
                    }
                    str_num++;
                }
                if (errors > 0)
                {
                    string message = "Скрипт попытался загрузить неверно оформленный файл Разговорника.\nСмотри в консоль за подробностями.";
                    if (errors * 2 >= str_num) message = "Скрипту не удалось прочесть Разговорник из файла. Скорее всего в настройках Перептолмачивателя выбран неверный Разделитель, либо загружаемый файл повереждён, или оформлен неверно.\nСмотри в консоль за подробностями."; // Если ошибки найдены в половине (или более) строк, то можно уверенно делать такой вывод.
                    EditorUtility.DisplayDialog("Ошибка!", message, "Ок");
                }
                else
                {
                    // Считываем:
                    tr.Close();
                    tr = new StreamReader(saveFilePath); //Возвращение в начало файла.
                    if (rewrite) Razgovornik.Clear();
                    while (true)
                    {
                        string str = tr.ReadLine();
                        if (str == null) break;
                        string[] readed = str.Split(sep, StringSplitOptions.None);
                        Razgovornik.Add(new DictElement(readed[0], readed[1]));
                    }
                }
                tr.Close();

                if (SortRazgovornik > 0) Razgovornik.Sort();
                if (RazgovornikContainer != null) RazgovornikContainer.Refresh();
            }
        }

        [MenuItem("Assets/Перетолмачить на инглиш")]
        private static void ShowWindow()
        {
            //wnd.maximized = false;
            //RenameSettingsWindow window = GetWindow<RenameSettingsWindow>(true, "Перептолмачивальня"); // Не смог добиться того же самого, но чтоб поддерживало мультипл виндоуз: там только одно открывает.
            RenameSettingsWindow window = CreateWindow<RenameSettingsWindow>("Перептолмачивальня");
            window.position = new Rect(Screen.currentResolution.width / 3, Screen.currentResolution.height * 0.2f, Screen.currentResolution.width / 3, Screen.currentResolution.height * 0.6f);
            window.Show();
            //window.ShowPopup();
        }

        public void OnEnable()
        {
            selected = new List<UnityEngine.Object>(Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets));

            Razgovornik = new AutoSortedDict { };
            settings = new RenameSettings(this);
            settings.SetDefaults(); // Это тут обязательно, на случай если не все из настроек будут в файле (допустим, я обновлю скрипт и добавлю какую-то настройку.)

            //Загрузка прошлых настроек (если они есть):
            LoadToSettings();
            LoadToRazgovornik();

        }

        public void RefreshSelection()
        {
            SelectedVisuals.Refresh();
        }

        private void PreventClosing()
        {
            // Unity не позволяет педотвратить закрытие или переопределить связанную с ним функцию...
            // То есть у неё есть стандартный EditorWindow.hasUnsavedChanges, но он не поддерживает более
            // сложные менюшки. Поэтому используем такой уродливый workaround:
            RenameSettingsWindow recreate_window = CreateWindow<RenameSettingsWindow>("Перептолмачивальня");
            recreate_window.ShowAuxWindow();
            recreate_window.position = position;
            recreate_window.RazgovornikChanged = RazgovornikChanged;

            recreate_window.selected = selected;
            recreate_window.RefreshSelection();
            recreate_window.Razgovornik = Razgovornik;

            recreate_window.settings = settings;
            recreate_window.RazgovornikPaddle = RazgovornikPaddle;
            recreate_window.AutoFindWordsPaddle = AutoFindWordsPaddle;
            recreate_window.OtherSettingsPaddle = OtherSettingsPaddle;
            recreate_window.Show();
            recreate_window.RazgovornikContainer.Refresh();
        }

        public void OnDisable()
        {
            selectedAssets_dragandrop.target.RemoveManipulator(selectedAssets_dragandrop);
            razgovornik_dragandrop.target.RemoveManipulator(selectedAssets_dragandrop); // Пожалуй излишне, учитывая что таргет находится в том же окне. Но... Хм.

            //Сохранение настроек и списков:
            if (RazgovornikChanged)
                switch (EditorUtility.DisplayDialogComplex("Закрытие", "Сохранить Разговорник?", "Сохранить", "Отмена", "Не сохранять"))
                {
                    case 0:
                        if (!SaveRazgovornik_with_window()) PreventClosing();
                        break;
                    case 1:
                        PreventClosing();
                        break;
                }
            SaveSettings();
        }


        private void RazgovornikControlls()
        {
            // Далее поддержка управления с клавиатуры (переключение между текстовыми полями):
            //
            // Из-за того, что текстовый ввод сам по себе использует стрелки-клавиши, события "съедаются"
            // и в принципе не могут быть использованы во время редактуры. Текущая реализация позволяет
            // переключать фокус только после завершения редактуры (нажатия Enter).
            //
            // Иииии... У меня не вышло решить проблему с автоматической активацией текстового ввода
            // вместо обычного выделения поля.
            Event current_event = Event.current;
            string focused_control = GUI.GetNameOfFocusedControl();

            if (focused_control.StartsWith("dictfield_"))
            {
                int j = Int32.Parse(focused_control.Substring(10, focused_control.Length - 10));
                int max_focus = (Razgovornik.Count + 1) * 2;

                if (focused_control == "dictfield_" + j)
                {
                    if (current_event.type == EventType.KeyDown)
                    {
                        int new_focus;
                        switch (current_event.keyCode)
                        {
                            case KeyCode.DownArrow:
                                new_focus = j + 2;
                                if (new_focus < max_focus)
                                {
                                    //GUI.changed = false;
                                    RazgovornikContainer.ScrollToItem(new_focus / 2);
                                    GUI.FocusControl("dictfield_" + new_focus);
                                    //root.focusController.focusedElement.Focus(); //Просто эксперименты...
                                    current_event.Use();

                                    /*
                                     Пытался решить баг с постоянным включением редактуры -- не сумел. Но суть кароче баги в том, что элементы в списке переиспользуются. Их там всего 10, и каждый 11 это самый первый выделенный. Поэтому на каждом 11-ом снова включается редактирование.
                                    TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                                    Debug.Log(editor);
                                    if (editor != null)
                                    {

                                        //GUI.changed = true;
                                        //GUIUtility.keyboardControl = 0; // Removes focus
                                        //editor.MoveCursorToPosition(editor.graphicalCursorPos);
                                        // To select all text: editor.SelectAll();
                                        // To move cursor to end: editor.MoveCursorToPosition(new Vector2(editor.position.width, editor.position.height));
                                    }*/

                                }
                                EditorGUIUtility.editingTextField = false;
                                break;
                            case KeyCode.UpArrow:
                                new_focus = j - 2;
                                if (new_focus >= 0)
                                {
                                    // Ой... Кароч не работает -_-...
                                    RazgovornikContainer.ScrollToItem(new_focus / 2);
                                    GUI.FocusControl("dictfield_" + new_focus);
                                    current_event.Use();
                                }
                                break;
                            case KeyCode.RightArrow:
                                new_focus = j + 1;
                                if (new_focus < max_focus)
                                {
                                    GUI.FocusControl("dictfield_" + new_focus);
                                    current_event.Use();
                                }
                                break;
                            case KeyCode.LeftArrow:
                                new_focus = j - 1;
                                if (new_focus >= 0)
                                {
                                    GUI.FocusControl("dictfield_" + new_focus);
                                    current_event.Use();
                                }
                                break;
                            case KeyCode.Return:
                                EditorGUI.FocusTextInControl(focused_control); // Не работает!
                                break;
                        }
                    }
                }
            }
        }

        public void CreateGUI()
        {
            //root = rootVisualElement;
            //selectedOption = EditorGUILayout.Popup("Select an option:", selectedOption, options);

            ScrollView MainArea = new ScrollView() { style = { alignSelf = Align.Stretch, alignItems = Align.Center } };


            //Предупреждение:
            var textElement = new TextElement();
            textElement.text = "ОБЯЗАТЕЛЬНО СДЕЛАЙ КОПИЮ ПРОЕКТА ИЛИ ХОТЯ БЫ ПАПКИ Assets ПЕРЕД ПРИМЕНЕНИЕМ!!!";
            textElement.style.maxWidth = 200;
            textElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            textElement.style.alignSelf = Align.Center;
            textElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            textElement.style.borderTopWidth = SpaceBetweenElements;
            textElement.style.borderBottomWidth = SpaceBetweenElements;
            MainArea.Add(textElement);
            //TemplateContainer t = new TemplateContainer();
            //t.style.unityTextAlign = TextAnchor.MiddleCenter;

            //t.Add(textElement);


            //Список выбранных файлов:
            MainArea.Add(new Label("Выбранные ассеты:")
            {
                style = { unityTextAlign = TextAnchor.MiddleCenter, unityFontStyleAndWeight = FontStyle.Bold }
            });

            SelectedVisuals = new AssetsList(selected, this); // Класс, формирующий список.
            selectedAssets_dragandrop = new SelectedAssetsDragndrop(SelectedVisuals.selectionList);// Драг-н-дроп для файлов.

            SelectedVisuals.selectionList.tooltip = "Перетаскивай ассеты драг-н-дропом, чтобы добавить их в список.\n\n(Выбор папки включает в себя так же и рекурсивную обработку всего её содержимого.)";
            MainArea.Add(SelectedVisuals.selectionList);


            // Настройки:
            MainArea.Add(new VisualElement() { style = { width = 500 } }); // Если честно, я так устал бодаться с юнитёвским гуи, что пусть этот элемент стоит просто как распорка, контролирующая максимальную ширину отображаемых настроек.
            IMGUIContainer SettingsConteiner = new IMGUIContainer() { onGUIHandler = SettingsPart_1 };
            MainArea.Add(SettingsConteiner);

            // Колонки Разговорника:
            RazgovornikInterface IRzgovornik = new RazgovornikInterface(Razgovornik); // интерфейс, чтобы выводить дополнительную строку (длина этого списка на 1 больше чем на самом деле).
            RazgovornikContainer = new ListViewAdvanced(IRzgovornik, (int)TextFieldHeight, () => new IMGUIContainer(), (VisualElement el, int i) => { (el as IMGUIContainer).onGUIHandler = DictListsOnGUI(i); }) { tooltip = "Перетаскивай файл сохранённого Разговорника драг-н-дропом, чтобы загрузить или добавить его содержимое.", style = { maxHeight = ListsHeight, minHeight = TextFieldHeight } };
            RazgovornikContainer.Refresh();
            RazgovornikContainer.name = "RazgovornikContainer";

            razgovornik_dragandrop = new RazgovornikFileDragndrop(RazgovornikContainer, this);// Драг-н-дроп для файлов.
            MainArea.Add(RazgovornikContainer);

            // Опять настройки: (костыльно разрезал их на две функции, чтобы добавить драг-н-дроп Разговорнику меж ними.)
            IMGUIContainer SettingsConteiner2 = new IMGUIContainer() { onGUIHandler = SettingsPart_2 };
            MainArea.Add(SettingsConteiner2);

            //Кнопки запуска:
            Button translate_button = new Button();
            translate_button.name = "translate";
            translate_button.text = "Заменить русизмы";
            translate_button.tooltip = "Находит все русские символы и заменяет их в соответствии с указанными настройками.";
            translate_button.clicked += () =>
            {
                if (EditorUtility.DisplayDialog("Точно?", "Файлы проекта будут необратимо изменены.\nТы сделала бэкап?",
                 "Да", "Отмена")) TranslateContent(true);
            };
            MainArea.Add(translate_button);

            Button find_button = new Button();
            find_button.name = "find";
            find_button.text = "Отыскать русизмы";
            find_button.tooltip = "Проверяет проект на наличие русских символов в соответствии с указанными настройками, после чего выводит результаты поиска в виде списка.";
            find_button.clicked += () => TranslateContent(false);
            MainArea.Add(find_button);

            MainArea.Add(new VisualElement() { style = { height = SpaceBetweenElements * 2 } });

            rootVisualElement.Add(MainArea);
        }

        private void SettingsPart_1()
        {
            GUIContent[] translate_types = {
            new GUIContent(
                "Простой транслит",
                "Съешь ещё этих мягких французских булок, да выпей же чаю. ==> Sesh eshchyo etih myagkih francuzskih bulok, da vypej zhe chayu."
                ),
            new GUIContent(
                "Разговорник",
                "Замена русизмов происходит в соответствии с вручную заполненным словарём. Если замены словаря не покрывают какую-либо русскую подпоследовательность русских символов, то она будет пропущена и в окне результатов появится соответствующая запись."
                ), // На самом деле это не словарь, в смысле структуры... Не Dictionary.
            new GUIContent(
                "Разговорник + транслит",
                "Сперва применяется поиск и замена по вручную заполненному словарю, а к оставшимся русизмам применяется \"простой транслит\"."
                ),
            /*new GUIContent(
                "Разговорник + Google Translate",
                "Для этого метода необходимо интернет-подключение. Обнаруженные русизмы будут заменены или в соответствии с вручную заполненным словарём, а при их отсутствии в оном, программа автоматически добавит в словарь результат с translate.google.com."
                )*/ // Оказалось слишком нетривиально. Отказался от этой идеи, не стал разбираться с ругистрацией API гугла.
        };
            settings.user_type = (UserType)EditorGUILayout.Popup(new GUIContent("Тип замены:"), (int)settings.user_type, translate_types);

            //UnityEngine.UIElements.PopupWindow ReplacementType = new UnityEngine.UIElements.PopupWindow();
            //ReplacementType.text = "Тип замены:";
            //ReplacementType.Add(new TextElement() { text = "Простой транслит", tooltip = "Съешь ещё этих мягких французских булок, да выпей же чаю. ==> Sesh eshchyo etih myagkih francuzskih bulok, da vypej zhe chayu." });
            //ReplacementType.Add(new TextElement() { text = "Замена по словарю", tooltip = "Замена русизмов происходит в соответствии с вручную заполненным словарём. Если слово отсутствует в словаре, то оно будет пропущено и в окне результатов появится соответствующая запись." });
            //ReplacementType.Add(new TextElement() { text = "Гугл переводчик", tooltip = "Для этого метода необходимо интернет-подключение. Обнаруженные русизмы будут заменены или в соответствии с вручную заполненным словарём, а при их отсутствии в оном, программа автоматически добавит в словарь результат с translate.google.com." });
            //w = EditorGUILayout.Popup(new Rect(0,0,0,0),"Select an option:", w, g);

            // Настройки:
            //TemplateContainer Ticks = new TemplateContainer(); // или Box();

            EditorGUILayout.Space(SpaceBetweenElements);
            settings.gameobjects_flag = EditorGUILayout.ToggleLeft(new GUIContent(
                "Обработать имена игровых объектов",
                "Если установлен этот флаг, то при поиске и замене будут учитываться имена объектов внутри сцен."
                ), settings.gameobjects_flag);

            if (settings.gameobjects_flag)
            {
                settings.recursive_prefs = EditorGUILayout.ToggleLeft(new GUIContent(
                    "Обрабатывать префабы рекурсивно",
                    "Если установлен этот флаг, то при поиске и замене в том числе будут обрабатываться непосредственно оригиналы префабов, а не только их представители на конкретной сцене."
                    ), settings.recursive_prefs);
            }

            settings.names_flag = EditorGUILayout.ToggleLeft(new GUIContent(
                "Обработать имена файлов",
                "Если установлен этот флаг, то при поиске и замене будут учитываться имена файлов.\n\nВ ТОМ ЧИСЛЕ ИМЕНА СКРИПТОВ! ДЛЯ ПЕРЕИМЕНОВАНИЯ СКРИПТОВ РЕКОМЕНДУЕТСЯ ДОПОЛНИТЕЛЬНО АКТИВИРОВАТЬ ОБРАБОТКУ ИХ СОДЕРЖИМОГО!"
                ), settings.names_flag);

            settings.code_flag = EditorGUILayout.ToggleLeft(new GUIContent(
                "Обработать код",
                "Если установлен этот флаг, то при поиске и замене будет учитываться содержимое скриптов.\n\nРекомендуется на всякий случай закрыть редакторы кода и обратить внимание, что замене подлежат в том числе и комментарии."
                ), settings.code_flag);
            EditorGUILayout.Space(7);


            if (settings.user_type > 0)
            {
                RazgovornikPaddle = EditorGUILayout.BeginFoldoutHeaderGroup(RazgovornikPaddle, "Разговорник: рус -> анг");
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            if (settings.user_type > 0 && RazgovornikPaddle)
            {
                RazgovornikContainer.style.display = DisplayStyle.Flex;
                RazgovornikContainer.SetEnabled(true);
            }
            else
            {
                RazgovornikContainer.style.display = DisplayStyle.None;
                RazgovornikContainer.SetEnabled(false);
            }
        }

        private void SettingsPart_2()
        {

            if (settings.user_type > 0 && RazgovornikPaddle)
            {
                EditorGUI.indentLevel++;
                string string_length_tooltip;
                if (settings.ActuallySortByLength) string_length_tooltip = "(она сейчас включена)";
                else string_length_tooltip = "(она сейчас отключена)";
                GUIContent[] sort_types = {
            new GUIContent(
                "Нет",
                "Если выбрана эта опция, то составляемый разговорник сортируется в порядке добавления элементов (в пределах текущей сессии)."
                ),
            new GUIContent(
                "По алфавиту",
                "Если выбрана эта опция, то составляемый разговорник будет автоматически сортироваться по алфавиту."
                ),
            new GUIContent(
                "По длине строк",
                "Если выбрана эта опция, то составляемый разговорник будет автоматические сортироваться по длине строк: от длинных к коротким, как это делает программа при реальном выполнении алгоритма, если включена опция \"Обрабатывать в порядке длины\" " + string_length_tooltip + "."
                ),
            };

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical(); // Тут Vertivcal Grouping потому-что без неё кнопка сохранения выглядит уродливо большой, а вертикальная группровка почему-то её... сжимает?..

                SortRazgovornik = (AutoSortedDict.SortType)EditorGUILayout.Popup(new GUIContent("Сортировать: "), (int)settings._sortRazgovornik, sort_types);
                EditorGUILayout.EndVertical();

                if (GUILayout.Button(EditorGUIUtility.IconContent("SaveActive").image)) SaveRazgovornik_with_window();

                EditorGUILayout.EndHorizontal();

                if (SortRazgovornik > 0) EditorGUILayout.HelpBox(
                    "Если ты сохранишь Разговорник с включённой сортировкой, оригинальный порядок элементов будет потерян.",
                    MessageType.Warning);

                settings.UseRegularExpr = EditorGUILayout.ToggleLeft(new GUIContent(
                    "Использовать регулярные выражения",
                    "При выборе этой опции левая колонка Разговорника будет интерпретироваться как регулярные выражения, а правая — как шаблон замены, который может включать в себя подстановки (см. Regex.Replace() и Substitutions).\n\nЭто требует более глубокого понимания и знакомства с языком регулярных выражений, краткий справочник к которому можно найти на сайте Microsoft.\n\nОбрати внимание на опцию \"Обрабатывать в порядке длины\". При использовании регулярных выражений рекомендуется оставить её включённой, но даже она не может гарантировать, что какой-либо из шаблонов случайно не окажется подпоследовательностью другого. Пример: \"[га]{1,2}\" и \"гагагу\". При сортировке по длине сперва будет обработано более длинное выражение, которое в строке \"гагагу\" заменит первые два слога \"****гу\", несмотря на то, что второе выражение соответсвовало бы всей строке целиком."
                    ), settings.UseRegularExpr);
                if (!settings.UseRegularExpr) settings.SubwordsInside = EditorGUILayout.ToggleLeft(new GUIContent(
                   "Разрешить вхождения слов в слова",
                   "Если включено, то алгоритм НЕ БУДЕТ отсекать случаи \"листопад -> leafопад\" или \"дворец -> дthiefец\".\n\nОбрати внимание, что эта опция неявно связана с настройкой \"Автопоиск слов\\Правило поиска\". Если в качестве правила выбран PascalCase, то названия по типу \"добрыйМаленькийГУСЁНОК\" будут восприняты как 3 отдельных слова (назависимо от того, включено или выключено \"Учитывать регистр букв\"!) Во всех остальных случаях для этой опции словами являются непрерывные последовательности русских символов целиком, независимо от буквенного регистра.\n\nЭта опция недоступна при выборе \"Использовать регулярные выражения\"."
                   ), settings.SubwordsInside);
                settings.LetterCase = EditorGUILayout.ToggleLeft(new GUIContent(
                    "Учитывать регистр букв",
                    "Если эта опция выключена, то поиск будет производиться вне зависимости от регистра букв, и замены будут по возможности соответствовать формату, задаваемому настройкой \"Соответствие буквенных регистров\" в разделе \"Другое\"."
                    ), settings.LetterCase);
                settings.ActuallySortByLength = EditorGUILayout.ToggleLeft(new GUIContent(
                   "Обрабатывать в порядке длины",
                   "Если включено, то при самом выполнении алгоритм отсортирует Разговорник в порядке длины элементов от длинных к коротким, как при выборе опции \"Сортировать по длине строк\".\n\nРекомендуется оставить включенным, чтобы алгоритм сперва заменял длинные последовательности, а затем более короткие: это может быть полезно при включении опции \"Использовать регулярные выражения\", так как снизит шанс ошибки если короткая последовательность окажется подпоследовательностью более длинной последовательности (но при этом может иметь и абсолютно обратный эффект!!!)\n\nПри отключении опции Разговорник будет обработан в том порядке, в каком он представлен в окне Перептолмачивателя."
                   ), settings.ActuallySortByLength);

                if (Razgovornik.Count > 0 && GUILayout.Button("Очистить Разговорник"))
                    if (EditorUtility.DisplayDialog("Ты уверена?", "Это действие нельзя будет отменить.",
                        "Очистить Разговорник", "Отмена"))
                    {
                        Razgovornik.Clear();
                        RazgovornikContainer.Refresh();
                    }

                EditorGUI.indentLevel--;
            }
            //EditorGUILayout.EndFoldoutHeaderGroup();

            AutoFindWordsPaddle = EditorGUILayout.BeginFoldoutHeaderGroup(AutoFindWordsPaddle, "Автопоиск слов:");
            if (AutoFindWordsPaddle)
            {
                EditorGUI.indentLevel++;
                GUIContent[] find_words_types = {
                new GUIContent(
                    "PascalCase",
                    "Поиск будет членить текст на куски, явно отделённые друг от друга сменой буквенного регистра. Пример: \"гусятняНомерОДИН\" будет разделена на \"гусятня\", \"Номер\" и \"ОДИН\". Но \"КРАСНЫЕЛапки\" в этом режиме будет разбито на \"КРАСНЫЕЛ\" и \"апки\". Для подобных случаев рекомендуется выбрать иное правило."
                    ),
                 new GUIContent(
                    "Только сплошной русский текст",
                    "Из текста будут выделяться куски, представляющие собой непрерывные последовательности русских символов. Например: \"Агисхьяльм\", \"гусятняНомерОДИН\", \"головамоямашетушамикаккрыльямиптицаейнашееногимаячитьбольшеневмочьчёрныйчеловекчёрныйчёрныйчёрныйчеловекнакроватькомнесадитсячёрныйчеловекспатьнедаетмневсюночь\""
                    ),
                new GUIContent(
                    "Один разделяющий символ",
                    "Поиск будет находить все русизмы, разделённые не более чем одним нерусским символом. Пример: \"гуси гуси га-га-га\", \"есть хотите\" и \"Да-да-да\". Обрати внимание, что за цельное слово будут приняты и математические выражения в скриптах, если в них есть русские символы: \"счёт=счёт+число_Побед\"."
                    ),
                new GUIContent(
                    "Два разделяющих символа",
                    "Поиск будет находить все русизмы, разделённые не более чем двумя нерусскими символами. Пример: \"гуси гуси га-га-га, есть хотите? Да-да-да\". Обрати внимание, что за цельное слово будут приняты и математические выражения в скриптах, если в них есть русские символы: \"счёт= счёт+число_Побед\"."
                    ),
                new GUIContent(
                    "Три разделяющих символа",
                    "Поиск будет находить все русизмы, разделённые не более чем тремя нерусскими символами. Пример: \"Если гусь оказался вкусь\", \"И не кис, и не гор, а — сол\". Обрати внимание, что за цельное слово будут приняты и математические выражения в скриптах, если в них есть русские символы: \"счёт = счёт + число_Побед\"."
                    ),
                new GUIContent(
                    "Четыре разделяющих символа",
                    "Поиск будет находить все русизмы, разделённые не более чем четырьмя нерусскими символами. Пример: \"Если гусь оказался вкусь... И не кис, и не гор, а — сол\". Обрати внимание, что за цельное слово будут приняты и математические выражения в скриптах, если в них есть русские символы: \"счёт += число_Побед\"."
                    ),
                new GUIContent(
                    "Пять разделяющих символов",
                    "По аналогии с предыдущими пунктами."
                    ),
                new GUIContent(
                    "Цельные предложения",
                    "Очень грубый метод поиска предложений на русском языке."
                    ),
                new GUIContent(
                    "Другое",
                    "Кастомный шаблон для поиска слов."
                    ),
            };
                settings.AutoFindWordsType = (AutoFindWordsEnum)EditorGUILayout.Popup(new GUIContent("Правило поиска:"), (int)settings.AutoFindWordsType, find_words_types); ;
                if (settings.AutoFindWordsType == AutoFindWordsEnum.OTHER)
                    settings.CustomFindRegex = EditorGUILayout.TextField(new GUIContent(
                            "Шаблон:",
                            "Регулярное выражение, по которому будет происходить автоматичекий поиск русизмов."
                            ), settings.CustomFindRegex);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            OtherSettingsPaddle = EditorGUILayout.BeginFoldoutHeaderGroup(OtherSettingsPaddle, "Другое:");
            if (OtherSettingsPaddle)
            {
                EditorGUI.indentLevel++;

                GUIContent[] CaseMaintainType_types = {
            new GUIContent(
                "Отключено",
                "Если выбрана эта опция, то соответствие буквенных регистров не будет браться в расчёт. (Конечный текст замен будет оставлен как есть, без попыток сохранить регистр. Т.е. замены, например, будут соответствовать тому, что вписано в правую колонку Разговорника.)"
                ),
            new GUIContent(
                "PascalCase с устойчивым последним регистром",
                "Если выбрана эта опция, регистр будет сохраняться следующим образом: \n\tГусятня -> Goosehouse (сохранение первой буквы большой)\n\tГУСЯТНЯ -> GOOSEHOUSE (сохранение всех букв большими)\n\tгусятня -> goosehouse (сохранение всех букв маленькими)\n\nПри этом каждая замена автоматически разбивается на составляющие, к которым будет применено это сохранение регистров (\"гусятняНомерОДИН\" будет разделена на \"гусятня\", \"Номер\" и \"ОДИН\") и тип регистра первого слова в замене будет соответствовать типу регистра первого слова в русизме. Если в замене меньше слов чем в русизме, то на нём выдержка регистра на нём и заканчивается. ЕСЛИ В ЗАМЕНЕ БОЛЬШЕ СЛОВ ЧЕМ В РУСИЗМЕ, ТО РЕГИСТР ОСТАВШИХСЯ СЛОВ СООТВЕТСТВУЕТ РЕГИСТРУ ПОСЛЕДНЕГО СЛОВА В РУСИЗМЕ."
                ),
            new GUIContent(
                "PascalCase со сравнением \"по кругу\"",
                "Если выбрана эта опция, регистр будет сохраняться следующим образом: \n\tГусятня -> Goosehouse (сохранение первой буквы большой)\n\tГУСЯТНЯ -> GOOSEHOUSE (сохранение всех букв большими)\n\tгусятня -> goosehouse (сохранение всех букв маленькими)\n\nПри этом каждая замена автоматически разбивается на составляющие, к которым будет применено это сохранение регистров (\"гусятняНомерОДИН\" будет разделена на \"гусятня\", \"Номер\" и \"ОДИН\") и тип регистра первого слова в замене будет соответствовать типу регистра первого слова в русизме. Если в замене меньше слов чем в русизме, то на нём выдержка регистра на нём и заканчивается. ЕСЛИ В ЗАМЕНЕ БОЛЬШЕ СЛОВ ЧЕМ В РУСИЗМЕ, ТО РЕГИСТР ОСТАВШИХСЯ СЛОВ СООТВЕТСТВУЕТ РЕГИСТРУ ПЕРВЫХ СЛОВ В РУСИЗМЕ, ПРОИЗВОДЯ СРАВНЕНИЕ ПО КРУГУ."
                ),
            };
                settings.CaseMaintainType = (CaseMaintainEnum)EditorGUILayout.Popup(new GUIContent("Сохранение регистра: ", "Эта настройка отвечает за то, каким образом при необходимости алгоритм будет пытаться выдерживать формат буквенного регистра при замене и при составлении автодополнения для Разговорника."), (int)settings.CaseMaintainType, CaseMaintainType_types);

                settings.ReplaceDashes = EditorGUILayout.ToggleLeft(new GUIContent(
                    "Заменить все \"-\" на \"_\"",
                    "Замена тире будет производиться только в названиях (файлов, папок и объектов на сценах), а так же в полях правой колонки Разговорника.\nК содержимому скриптов эта замена по понятным причинам применена не будет. Если нужны изменения в кодовой части, то рекомендуется делать это вручную, ориентируясь по консольным ошибкам после применения Перептолмачивателя."
                    ), settings.ReplaceDashes);
                settings.ReplaceSpaces = EditorGUILayout.ToggleLeft(new GUIContent(
                    "Заменить все \" \" на \"_\"",
                    "Замена пробелов будет производиться только в названиях (файлов, папок и объектов на сценах), а так же в полях правой колонки Разговорника.\nК содержимому скриптов эта замена по понятным причинам применена не будет. Если нужны изменения в кодовой части, то рекомендуется делать это вручную, ориентируясь по консольным ошибкам после применения Перептолмачивателя."
                    ), settings.ReplaceSpaces);
                GUIContent[] save_load_types = {
                new GUIContent(
                    "Текстовый файл",
                    "Разговорник сохраняется в виде текстового файла в две колонки, разделённых произвольно задаваемым разделителем. Может быть отредактирован блокнотом."
                    ),
                new GUIContent(
                    "Таблица",
                    "Сохраняет и загружает Разговорник в виде .csv-файла, который может быть открыт и отредактирован любым редактором вроде Excel или LibreOffice Calc.\n\nИмеет ряд недостатков, например — невозможность записи одиночной кавычки, или конфликт с разделителем, если в содержимом Разговорника встречаются запятые или вертикальные чёрточки.\n\nВАЖНО: при использовании редактора таблиц требуется для импорта и сохранения файла указать кодировку Юникод (UTF-8)."
                    )
            };
                settings.SaveLoadType = (SaveLoadEnum)EditorGUILayout.Popup(new GUIContent("Формат Разговорника:"), (int)settings.SaveLoadType, save_load_types);
                switch (settings.SaveLoadType)
                {
                    case SaveLoadEnum.TXT_FILE:
                        settings.text_separator = EditorGUILayout.TextField(new GUIContent(
                            "Разделитель:",
                            "Произвольная последовательность символов, которая будет использоваться в качестве разделителя при записи и чтении Разговорника из файла. Главное — чтобы эта последовательность не встречалась в содержимом самого разговорника. Например: \"{separator}\"."
                            ), settings.text_separator);
                        break;
                    case SaveLoadEnum.CSV_TABLE:
                        settings.spreadsheet_separator = EditorGUILayout.Popup(new GUIContent("Разделитель:"), settings.spreadsheet_separator, spreadsheet_separator_types_guitext);
                        if (settings.spreadsheet_separator >= spreadsheet_separator_types.Length)
                            settings.text_separator = EditorGUILayout.TextField(new GUIContent(
                                " ",
                                "Произвольная последовательность символов, которая будет использоваться в качестве разделителя при записи и чтении Разговорника из файла. Главное — чтобы эта последовательность не встречалась в содержимом самого разговорника. Например: \"{separator}\"."
                                    ), settings.text_separator);
                        break;
                }
                settings.separate_strings_text = EditorGUILayout.ToggleLeft(new GUIContent(
                    "Обрабатывать многострочные тексты построчно",
                    "Если выбрано, то каждая строка многострочных текстов будет обрабатываться по отдельности, замены на стыке строк обнаруживаться не будут.\n\n(На данный момент такими текстами являются исключительно скрипты. Рекомендуется оставить включённым для сохранения номеров строк с найденными русизмами.)"
                    ), settings.separate_strings_text);
                settings.logsContextSize = Math.Max(0, EditorGUILayout.IntField(new GUIContent(
                    "Размер контекста",
                    "Максимальное число символов, дополнительно захватываемых справа и слева от каждого вхождения для демонстрации контекста в отчёте о результатах."),
                    settings.logsContextSize));

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }



        void TranslateContent(bool make_changes)
        {
            new RenameActor(this, make_changes);
        }

        /*
        [MenuItem("MyMenu/Log Selected Transform Name", true)]
        static bool ValidateLogSelectedTransformName()
        {
            // Return false if no transform is selected.
            return Selection.activeTransform != null;
        }
         */

    }
}