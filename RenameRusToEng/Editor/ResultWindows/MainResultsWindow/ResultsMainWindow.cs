using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace RenameRusToEng
{

    /// <summary>
    /// Графическая репрезентация результатов работы Перептолмачивателя.
    /// </summary>
    class ResultsMainWindow : EditorWindow
    {
        RenameActor actor;
        RenameSettingsWindow root_settings_window;
        public static Color obvodka_color = new Color(0, 0, 0, 1);

        public const float labelfontsize = 15;
        public const float SpaceBetweenElements = RenameSettingsWindow.SpaceBetweenElements / 2;

        public void SetData(RenameActor _actor)
        {
            actor = _actor;
            root_settings_window = actor.root_settings_window;
            CreateGUI_delayed();
        }

        public void CreateGUI_delayed() // Вместо обычного CreateGUI, просто потому что Unity не даёт мне одновременно передать при создании необходимые для окна данные и в то же время прикрепить вкладку к окну-источнику (а прикрепить, по видимому, можно только при создании.)
        {
            ScrollView MainScrollArea = new ScrollView();
            //selectedOption = EditorGUILayout.Popup("Select an option:", selectedOption, options);
            float ListsHeight = RenameSettingsWindow.ListsHeight;


            string label_1 = "";
            string label_2 = "";

            switch (actor.current_settings.user_type)
            {
                case RenameSettingsWindow.UserType.TRANSLIT:
                    label_1 = "Транслит:";
                    break;
                case RenameSettingsWindow.UserType.RAZGOVORNIK:
                    label_1 = "Разговорник:";
                    label_2 = "Остальные вхождения (автопоиск):";
                    break;
                case RenameSettingsWindow.UserType.RAZGOVORNIK_TRANSLIT:
                    label_1 = "Разговорник:";
                    label_2 = "Транслит:";
                    break;
                    //case RenameSettingsWindow.UserType.RAZGOVORNIK_TRANSLATE:
                    //    label_1 = "Разговорник:";
                    //    label_2 = "Автоперевод:";
                    //    break;
            }


            // Первый список:
            LogsList list1 = new LogsList(label_1, ListsHeight);
            foreach (ObjectSubstitutionInfo info in actor.ProvidedSubstitutions)
            {
                objectLogsRepresentation obj_represent = new objectLogsRepresentation(info);
                list1.Add(obj_represent);
            }
            MainScrollArea.Add(list1);


            // Второй список:
            LogsList list2 = new LogsList(label_2, ListsHeight);
            foreach (ObjectSubstitutionInfo info in actor.AdditionSubstitutions)
            {
                objectLogsRepresentation obj_represent = new objectLogsRepresentation(info);
                list2.Add(obj_represent);
            }
            MainScrollArea.Add(list2);


            // Третий список (если есть, то всегда "Автодополнение к Разговорнику")
            if (actor.current_settings.user_type == RenameSettingsWindow.UserType.RAZGOVORNIK) // || actor.current_settings.user_type == RenameSettingsWindow.UserType.RAZGOVORNIK_TRANSLATE)
            {
                AdditionRazgovornikList list3 = new AdditionRazgovornikList("Автодополнение к Разговорнику:", ListsHeight, actor.AutoRazgovornik);
                MainScrollArea.Add(list3);

                Button AddWordsButton = new Button();
                AddWordsButton.name = "add_words";
                AddWordsButton.text = "Добавить в Разговорник";
                AddWordsButton.tooltip = "Эта кнопка добавляет сформированный выше список найденных слов в Разговорник в изначальном окне.";
                AddWordsButton.style.width = 250;
                AddWordsButton.style.alignSelf = Align.Center;
                AddWordsButton.clicked += () =>
                {
                    foreach (DictElement word in actor.AutoRazgovornik)
                    {
                        root_settings_window.Razgovornik.Add(new DictElement(word.rus, word.eng));
                    }
                    if (root_settings_window.SortRazgovornik > 0) root_settings_window.Razgovornik.Sort();
                    if (root_settings_window.RazgovornikContainer != null) root_settings_window.RazgovornikContainer.Refresh();

                    root_settings_window.RazgovornikChanged = true;
                //root_settings_window.Show();
                root_settings_window.Focus();
                    EditorUtility.DisplayDialog("Гуси научились говорить, миледи!", "Найденные русизмы добавлены в Разговорник.", "Ок");
                };
                VisualElement simple_spacing_element = new VisualElement() { style = { borderTopWidth = SpaceBetweenElements, borderBottomWidth = SpaceBetweenElements * 2 } };
                simple_spacing_element.Add(AddWordsButton);
                MainScrollArea.Add(simple_spacing_element);

            }

            rootVisualElement.Add(MainScrollArea);
        }
    }
}