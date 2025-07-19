// Умрике от Смекты )
//
// Инструмент для массовой коррекции текстов в проектах на Unity (v2020.3.14): переименование файлов и папок,
// игровых объектов, замена строковых вхождений внутри скриптов. В частности — для поиска и замены наименований, содержащих
// кириллицу. Имеет ряд настроек, ориентированных на работу с естественным языком, а так же на использование PascalCase,
// но может так же использоваться для поиска и замены любых иных строковых вхождений.
//
//   Скрипт поддерживает:
// ○ режим работы с регулярными выражениями
// ○ импорт/экспорт входных данных в форматах .txt и .cfg
// ○ вывод на экран пошагового отчёта о ходе выполнения алгоритма замены.
//
//  Установка и использование:
// 1. Создать в папке Assets вашего проекта папку Editor, если её там нет.
// 2. Поместить данный скрипт внутрь папки Editor.
// 3. После загрузки скрипта выберите в инспекторе Unity интересующие вас файлы и папки, щёлкните по ним
//    правой кнопкой мыши, и в контекстном меню отыщите появившийся пункт "Перетолмачить на инглиш".
// 4. Далее руководствуйтесь подсказками интерфейса.
//
// Написан С.Смекты с использованием Unity-v2020.3.14 в 2025 году.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.SceneManagement;
using System.Collections;

public class ListViewAdvanced : ListView // В некотором роде исправленная версия ListView, растягивающая его как положено.
{
    public ListViewAdvanced(IList itemsSource, int itemHeight, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem) : base(itemsSource, itemHeight, makeItem, bindItem) {}

    public new void Refresh()
    {
        RefreshSize();
        base.Refresh();
    }

    public void RefreshSize()
    {
        style.height = Mathf.Clamp(itemHeight * itemsSource.Count, style.minHeight.value.value, style.maxHeight.value.value);
    }
}

class AssetInList : TemplateContainer
{
    public int index;

    public AssetInList()
    {
        style.flexDirection = FlexDirection.Row;
        Add(new Image() { scaleMode = ScaleMode.ScaleToFit, style = { width = 15, height = 15, alignContent = Align.Center } });
        Add(new Label());
        Button delete_button = new Button();
        delete_button.Add(new Image() {
            image = EditorGUIUtility.IconContent("d_TreeEditor.Trash").image,
            scaleMode = ScaleMode.ScaleToFit,
            style = {
                width = 15,
                height = 15,
                alignContent = Align.Center,
                alignSelf = Align.FlexEnd
            }
        });

        delete_button.clicked += DeleteItem;
        TemplateContainer button_alligment_container = new TemplateContainer();
        button_alligment_container.style.flexDirection = FlexDirection.RowReverse;
        button_alligment_container.style.alignItems = Align.FlexEnd;
        button_alligment_container.style.flexGrow = 1;
        button_alligment_container.Add(delete_button);

        Add(button_alligment_container);
    }

    private void DeleteItem()
    {
        ListView list = parent.parent as ListView;
        list.itemsSource.RemoveAt(index);
        list.Refresh();
    }
}



abstract class FilesDragAndDropManipulator : PointerManipulator
{
    // https://docs.unity3d.com/6000.1/Documentation/Manual/UIE-drag-across-windows.html#:~:text=From%20the%20menu%2C%20select%20Window,from%20one%20window%20to%20another.
    // https://docs.unity3d.com/2020.1/Documentation/Manual/UIE-Events-DragAndDrop.html
    //
    // Можно это и в OnGui() проделывать, через
    // if (Event.current.type == EventType.MouseDrag) и Rect.Contains(Event.current.mousePosition),
    // но это тогда будет ЕЩЁ ЗАБОРИСТЕЕ (так что предпочтительнее обойтись этими
    // пятью функциями класса PointerManipulator.)

    bool draging_files = false;

    public FilesDragAndDropManipulator(VisualElement root)
    {
        target = root;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<DragEnterEvent>(OnDragEnter);
        target.RegisterCallback<DragLeaveEvent>(OnDragLeave);
        target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
        target.RegisterCallback<DragPerformEvent>(OnDragPerform);
        target.RegisterCallback<DragExitedEvent>(OnDragExited);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<DragEnterEvent>(OnDragEnter);
        target.UnregisterCallback<DragLeaveEvent>(OnDragLeave);
        target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
        target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
        target.UnregisterCallback<DragExitedEvent>(OnDragExited);
    }

    public abstract bool DragFileFilter(); // Описан в наследниках, т.к. мне нужно два разных подобных класса.

    void OnDragEnter(DragEnterEvent _)
    {
        draging_files = DragFileFilter();
        if (draging_files) target.AddToClassList("drop-area--dropping");
    }

    void OnDragLeave(DragLeaveEvent _)
    {
        EndDrug();
    }

    void OnDragUpdate(DragUpdatedEvent _)
    {
        if (draging_files) DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
    }

    public abstract void OnDragPerform(DragPerformEvent _); // Описан в наследниках, т.к. мне нужно два разных подобных класса.

    void OnDragExited(DragExitedEvent _)
    {
        EndDrug();
    }

    public void EndDrug()
    {
        target.RemoveFromClassList("drop-area--dropping");
    }
}


class SelectedAssetsDragndrop : FilesDragAndDropManipulator
{
    public SelectedAssetsDragndrop(VisualElement root) : base(root) { }

    public override bool DragFileFilter()
    {
        foreach (UnityEngine.Object droppable in DragAndDrop.objectReferences)
        {
            if (AssetDatabase.Contains(droppable)) return true;
        }
        return false;
    }

    public override void OnDragPerform(DragPerformEvent _)
    {
        ListView list = (target as ListView);
        UnityEngine.Object last_added = null;
        foreach (UnityEngine.Object droppable in DragAndDrop.objectReferences)
        {
            if (AssetDatabase.Contains(droppable))
            {
                if (!list.itemsSource.Contains(droppable)) list.itemsSource.Add(droppable);
                last_added = droppable;
            }
        }

        list.Refresh();
        int last_indx = DragAndDrop.objectReferences.Length - 1;
        list.ScrollToItem(list.itemsSource.IndexOf(last_added));
        EndDrug();
    }
}


class RazgovornikFileDragndrop : FilesDragAndDropManipulator
{
    RenameSettings Pereptolmachivatel;

    public RazgovornikFileDragndrop(VisualElement root_widget, RenameSettings root_window) : base(root_widget)
    {
        Pereptolmachivatel = root_window;
    }

    public override bool DragFileFilter() => DragAndDrop.objectReferences.Length == 1 && AssetDatabase.Contains(DragAndDrop.objectReferences[0]);

    public override void OnDragPerform(DragPerformEvent _)
    {
        int choose = EditorUtility.DisplayDialogComplex("Загрузка Разговорника", "Ты можешь добавить данные из файла в Разговорник, а можешь заменить их в нём.", "Добавить", "Отмена", "Заменить");
        switch (choose)
        {
            case 0:
                Pereptolmachivatel.LoadToRazgovornik(false, AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]));
                break;
            case 2:
                Pereptolmachivatel.LoadToRazgovornik(true, AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]));
                break;
            case 1:
                break;
        }
        EndDrug();
    }
}


public class RenameSettings : EditorWindow
{

    public class DictElement
    {
        public int order;
        public string rus;
        public string eng;
        public DictElement(string a, string b)
        {
            rus = a;
            eng = b;
        }
    }


    public class AutoSortedDict : List<DictElement>
    {
        private int order_counter = 0;

        private IComparer<DictElement> ActualComparer = new DictOrderComparator();
        private int _sortRazgovornik;
        public int SortRazgovornik
        {
            get => _sortRazgovornik;
            set
            {
                if (value != _sortRazgovornik)
                {
                    _sortRazgovornik = value;
                    switch (value)
                    {
                        case 0:
                            ActualComparer = new DictOrderComparator();
                            break;
                        case 1:
                            ActualComparer = new DictAlphabetComparator();
                            break;
                        case 2:
                            ActualComparer = new DictLengthComparator();
                            break;
                    }
                    Sort();
                }
            }
        }

        private class DictOrderComparator : IComparer<DictElement>
        {
            int IComparer<DictElement>.Compare(DictElement a, DictElement b)
            {
                return a.order - b.order;
            }
        }

        private class DictAlphabetComparator : IComparer<DictElement>
        {
            int IComparer<DictElement>.Compare(DictElement a, DictElement b)
            {
                int diff = string.Compare(a.rus, b.rus, StringComparison.CurrentCulture);
                if (diff == 0)
                {
                    diff = string.Compare(a.eng, b.eng, StringComparison.CurrentCulture);
                    if (a.eng == "" || b.eng == "") return -diff; //Отдельная обработка пустой строки чтоб все пустые строки были в конце.
                }
                else
                    if (a.rus == "" || b.rus == "") return -diff; //Отдельная обработка пустой строки чтоб все пустые строки были в конце.
                return diff;
            }
        }

        public static int CompareLengthAndAlphabet(string a, string b)
        {
            int diff = b.Length - a.Length;
            if (diff == 0)
            {
                diff = string.Compare(a, b, StringComparison.CurrentCulture); // Для однозначности дополнительно сортируем по алфавиту.
            }
            return diff;
        }

        private class DictLengthComparator : IComparer<DictElement>
        {
            int IComparer<DictElement>.Compare(DictElement a, DictElement b)
            {
                int diff = CompareLengthAndAlphabet(a.rus, b.rus);
                if (diff == 0)
                {
                    diff = CompareLengthAndAlphabet(a.eng, b.eng);
                }
                return diff;
            }
        }
        
        private void GeneralInit()
        {
            SortRazgovornik = 0;
        }

        public AutoSortedDict() : base() { GeneralInit(); }
        public AutoSortedDict(List<DictElement> list) : base(list) { GeneralInit(); }

        public new void Add(DictElement item)
        {
            item.order = order_counter;
            order_counter++;
            base.Add(item);
        }

        public int AddSorted(DictElement item)
        {
            item.order = order_counter;
            order_counter++;
            return PutSorted(item);
        }

        public int PutSorted(DictElement item)
        {
            int index_to_insert = BinarySearch(item, ActualComparer);
            if (index_to_insert < 0) index_to_insert = ~index_to_insert;

            Insert(index_to_insert, item);
            return index_to_insert;
        }

        public new void Sort()
        {
            Sort(ActualComparer);
        }

        public int ResortElement(int item_index)
        {
            DictElement item = this[item_index];
            RemoveAt(item_index);
            return PutSorted(item);
        }
    }


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

    public class Settings
    {
        private RenameSettings root = null;

        public int user_type; // 0 = Транслит, 1 = разговорник, 2 = разговорник + транслит

        public bool gameobjects_flag;
        public bool recursive_prefs;
        public bool names_flag;
        public bool code_flag;

        public int _sortRazgovornik;
        public bool UseRegularExpr;
        public bool LetterCase;
        public bool ActuallySortByLength;
        public bool SubwordsInside;

        public int AutoFindWordsType;
        public string CustomFindRegex;

        public int CaseMaintainType;
        public bool ReplaceDashes;
        public bool ReplaceSpaces;
        public int SaveLoadType;
        public string text_separator;
        public int spreadsheet_separator;
        public bool separate_strings_text;
        public int logsContextSize;

        public Settings(RenameSettings _root)
        {
            root = _root;
        }

        public Settings Clone()
        {
            return (Settings)MemberwiseClone();
        }

        public void SetDefaults()
        {
            user_type = 1; // словарь

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

            CaseMaintainType = 1;
            ReplaceDashes = false;
            ReplaceSpaces = false;
            SaveLoadType = 0; // текстовый файл
            text_separator = " ☚►❤◄☛ ";
            spreadsheet_separator = 0; // ", "
            separate_strings_text = true;
            logsContextSize = 15;
        }
    }

    public Settings settings;

    public int SortRazgovornik // локальный управлятор, чтобы и в сохраняемый класс значение внести, и Разговорник отсортировать.
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
        if (sourceFilePath == "") sourceFilePath = Application.dataPath + Path.DirectorySeparatorChar + "Editor" + Path.DirectorySeparatorChar + "FromRusToEng_Toolset";
        else sourceFilePath = Path.GetDirectoryName(sourceFilePath);
        return sourceFilePath;
    }

    private string GetDefaultSettingsSavePath() => Path.Combine(GetDefaultSavePath(), "RRTE_Settings.json");

    private string GetRazgovornikExtension()
    {
        switch (settings.SaveLoadType)
        {
            case 0: return "txt";
            case 1: return "csv";
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

    int RazgovornikAutoSaveType(string saveFilePath)
    {
        switch (Path.GetExtension(saveFilePath))
        {
            case ".txt": return 0;
            case ".csv": return 1;
            default: return -1; // Сигнализирует об ошибке.
        }
    }

    private string[] GetRazgovornikSep(int SaveLoadType_current)
    {
        switch (SaveLoadType_current)
        {
            case 0:
                return new string[] { settings.text_separator };
            case 1:
                if (settings.spreadsheet_separator < spreadsheet_separator_types.Length)
                    return new string[] { spreadsheet_separator_types[settings.spreadsheet_separator] };
                else return new string[] { settings.text_separator };
            default: return new string[] { settings.text_separator };
        }
    }


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
        int SaveLoadType_current;
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
            else RazgovornikChanged = false;
        }

        string sep = GetRazgovornikSep(SaveLoadType_current)[0];

        TextWriter tw = new StreamWriter(saveFilePath, false);

        foreach (DictElement i in Razgovornik) tw.WriteLine(i.rus + sep + i.eng);

        tw.Close();
    }

    public void LoadToRazgovornik(bool rewrite = false, string saveFilePath = "")
    {
        int SaveLoadType_current;
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
                    Debug.LogError("Ошибка в оформлении файла Разговорника: строка " + str_num+", вместо двух элементов обнаружено "+ elements_in_string + ".\nВ качестве разделителя использованы:" + sep + "\n\n" + str + "\n\n" + saveFilePath + "\n\n");
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
        //RenameSettings window = GetWindow<RenameSettings>(true, "Перептолмачивальня"); // Не смог добиться того же самого, но чтоб поддерживало мультипл виндоуз: там только одно открывает.
        RenameSettings window = CreateWindow<RenameSettings>("Перептолмачивальня");
        window.position = new Rect(Screen.currentResolution.width / 3, Screen.currentResolution.height * 0.2f, Screen.currentResolution.width / 3, Screen.currentResolution.height * 0.6f);
        window.Show();
        //window.ShowPopup();
    }

    public void OnEnable()
    {
        selected = new List<UnityEngine.Object>(Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets));

        Razgovornik = new AutoSortedDict { };
        settings = new Settings(this);
        settings.SetDefaults(); // Это тут обязательно, на случай если не все из настроек будут в файле (допустим, я обновлю скрипт и добавлю какую-то настройку.)

        //Загрузка прошлых настроек (если они есть):
        LoadToSettings();
        LoadToRazgovornik();

    }

    private void PreventClosing()
    {
        // Unity не позволяет педотвратить закрытие или переопределить связанную с ним функцию...
        // То есть у неё есть стандартный EditorWindow.hasUnsavedChanges, но он не поддерживает более
        // сложные менюшки. Поэтому используем такой уродливый workaround:
        RenameSettings recreate_window = CreateWindow<RenameSettings>("Перептолмачивальня");
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

    private ListView selectionList;
    public void RefreshSelection()
    {
        selectionList.Refresh();
    }

    class RazgovornikInterface : IList
    {
        IList Razgovornik;

        public RazgovornikInterface(IList r) { Razgovornik = r; }

        public object this[int index] { get => null; set => Razgovornik[index] = value; }

        public bool IsFixedSize => Razgovornik.IsFixedSize;

        public bool IsReadOnly => Razgovornik.IsReadOnly;

        public int Count => Razgovornik.Count + 1;

        public bool IsSynchronized => Razgovornik.IsSynchronized;

        public object SyncRoot => Razgovornik.SyncRoot;

        public int Add(object value)
        {
            return Razgovornik.Add(value);
        }

        public void Clear()
        {
            Razgovornik.Clear();
        }

        public bool Contains(object value)
        {
            return Razgovornik.Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            Razgovornik.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return Razgovornik.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return Razgovornik.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            Razgovornik.Insert(index, value);
        }

        public void Remove(object value)
        {
            Razgovornik.Remove(value);
        }

        public void RemoveAt(int index)
        {
            Razgovornik.RemoveAt(index);
        }
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
            style = { unityTextAlign = TextAnchor.MiddleCenter, unityFontStyleAndWeight = FontStyle.Bold}
        });


        const int itemHeight = 18;

        Func<VisualElement> makeItem = () => new AssetInList();
        Action<VisualElement, int> binditem = (element, indx) =>
        {
            (element.contentContainer[0] as Image).image = AssetPreview.GetMiniThumbnail(selected[indx]);
            (element.contentContainer[1] as Label).text = AssetDatabase.GetAssetPath(selected[indx]);
            (element as AssetInList).index = indx;
        };

        selectionList = new ListView(selected, itemHeight, makeItem, binditem);
        selectionList.style.height = 140;//ListsHeight;
        selectionList.selectionType = SelectionType.None;
        selectionList.showBorder = true;
        selectionList.showBoundCollectionSize = false;
        selectionList.onItemsChosen += items => { foreach (UnityEngine.Object item in items) {Selection.SetActiveObjectWithContext(item, this);} };
        selectedAssets_dragandrop = new SelectedAssetsDragndrop(selectionList);// Драг-н-дроп для файлов.

        TemplateContainer selectionBox = new TemplateContainer();
        selectionBox.tooltip = "Перетаскивай ассеты драг-н-дропом, чтобы добавить их в список.\n\n(Выбор папки включает в себя так же и рекурсивную обработку всего её содержимого.)";
        selectionBox.Add(selectionList);
        MainArea.Add(selectionBox);


        // Настройки:
        MainArea.Add(new VisualElement() { style = { width = 500 } }); // Если честно, я так устал бодаться с юнитёвским гуи, что пусть этот элемент стоит просто как распорка, контролирующая максимальную ширину отображаемых настроек.
        IMGUIContainer SettingsConteiner = new IMGUIContainer() { onGUIHandler = SettingsPart_1};
        MainArea.Add(SettingsConteiner);

        // Колонки Разговорника:
        RazgovornikInterface IRzgovornik = new RazgovornikInterface(Razgovornik); // интерфейс, чтобы выводить дополнительную строку (длина этого списка на 1 больше чем на самом деле).
        RazgovornikContainer = new ListViewAdvanced(IRzgovornik, (int)TextFieldHeight, () => new IMGUIContainer(), (VisualElement el, int i) => { (el as IMGUIContainer).onGUIHandler = DictListsOnGUI(i); }) {tooltip = "Перетаскивай файл сохранённого Разговорника драг-н-дропом, чтобы загрузить или добавить его содержимое.", style = { maxHeight = ListsHeight, minHeight = TextFieldHeight } };
        RazgovornikContainer.Refresh();
        RazgovornikContainer.name = "RazgovornikContainer";

        razgovornik_dragandrop = new RazgovornikFileDragndrop(RazgovornikContainer, this);// Драг-н-дроп для файлов.
        MainArea.Add(RazgovornikContainer);

        // Опять настройки: (костыльно разрезал их на две функции, чтобы добавить драг-н-дроп Разговорнику.)
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

        MainArea.Add(new VisualElement() { style = { height = SpaceBetweenElements * 2} });

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
                )*/
        };
        settings.user_type = EditorGUILayout.Popup(new GUIContent("Тип замены:"), settings.user_type, translate_types);

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

            SortRazgovornik = EditorGUILayout.Popup(new GUIContent("Сортировать: "), settings._sortRazgovornik, sort_types);
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
            settings.AutoFindWordsType = EditorGUILayout.Popup(new GUIContent("Правило поиска:"), settings.AutoFindWordsType, find_words_types); ;
            if (settings.AutoFindWordsType == find_words_types.Length - 1)
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
                "PascalCase со сравнением \"по кругу\'",
                "Если выбрана эта опция, регистр будет сохраняться следующим образом: \n\tГусятня -> Goosehouse (сохранение первой буквы большой)\n\tГУСЯТНЯ -> GOOSEHOUSE (сохранение всех букв большими)\n\tгусятня -> goosehouse (сохранение всех букв маленькими)\n\nПри этом каждая замена автоматически разбивается на составляющие, к которым будет применено это сохранение регистров (\"гусятняНомерОДИН\" будет разделена на \"гусятня\", \"Номер\" и \"ОДИН\") и тип регистра первого слова в замене будет соответствовать типу регистра первого слова в русизме. Если в замене меньше слов чем в русизме, то на нём выдержка регистра на нём и заканчивается. ЕСЛИ В ЗАМЕНЕ БОЛЬШЕ СЛОВ ЧЕМ В РУСИЗМЕ, ТО РЕГИСТР ОСТАВШИХСЯ СЛОВ СООТВЕТСТВУЕТ РЕГИСТРУ ПЕРВЫХ СЛОВ В РУСИЗМЕ, ПРОИЗВОДЯ СРАВНЕНИЕ ПО КРУГУ."
                ),
            };
            settings.CaseMaintainType = EditorGUILayout.Popup(new GUIContent("Сохранение регистра: ", "Эта настройка отвечает за то, каким образом при необходимости алгоритм будет пытаться выдерживать формат буквенного регистра при замене и при составлении автодополнения для Разговорника."), settings.CaseMaintainType, CaseMaintainType_types);

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
            settings.SaveLoadType = EditorGUILayout.Popup(new GUIContent("Формат Разговорника:"), settings.SaveLoadType, save_load_types);
            switch (settings.SaveLoadType)
            {
                case 0:
                    settings.text_separator = EditorGUILayout.TextField(new GUIContent(
                        "Разделитель:",
                        "Произвольная последовательность символов, которая будет использоваться в качестве разделителя при записи и чтении Разговорника из файла. Главное — чтобы эта последовательность не встречалась в содержимом самого разговорника. Например: \"{separator}\"."
                        ), settings.text_separator);
                    break;
                case 1:
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
            settings.logsContextSize = Math.Max(0,EditorGUILayout.IntField(new GUIContent(
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
        //CreateWindow<ResultsMainWindow>().Show();
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




    class RenameActor
{
    
    // ФУНКЦИОНАЛЬНАЯ ЧАСТЬ КОДА:

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
    Regex SmallNativeLetterBRegex = new Regex(@"\G[" + NativeSmall+"]", RegexOptions.Compiled);
    Regex BigNativeLetterERegex = new Regex("["+NativeCaps + @"]\G", RegexOptions.RightToLeft | RegexOptions.Compiled);
    Regex SmallNativeLetterERegex = new Regex("[" + NativeSmall + @"]\G", RegexOptions.RightToLeft | RegexOptions.Compiled);
    Regex NativeLetterBRegex = new Regex(@"\G[" + NativeAlphabet + "]", RegexOptions.Compiled);
    Regex NativeLetterERegex = new Regex("[" + NativeAlphabet + @"]\G", RegexOptions.RightToLeft | RegexOptions.Compiled);
    public RenameSettings root_settings_window;
    public bool make_changes; // Если false, то программа не будет вносить реальных изменений в файлы проекта, а только найдёт русизмы.

    RenameSettings.Settings current_settings; // Настройки, при которых запущен процесс перетолмачивания (копируются с менюшки).


    class PreparedDictElement
    {
        public string original_rus;
        public string original_eng;
        public Regex Regex_rus;
        public string Regex_eng;
    }

    List<PreparedDictElement> ProvidedRazgovornik = new List<PreparedDictElement>();
    RenameSettings.AutoSortedDict AutoRazgovornik = new RenameSettings.AutoSortedDict();
    Dictionary<string, RenameSettings.DictElement> AutoFoundWords = new Dictionary<string, RenameSettings.DictElement>(); // Словарь для быстрой проверки наличия слов в списке.

    class LastMatchesInfo // Вспомогательный класс для составления отчётных логов о заменах.
    {
        public PreparedDictElement dict_element;
        public Match match;
        public string replacement;

        public LastMatchesInfo(PreparedDictElement d_e, Match m, string r)
        {
            dict_element = d_e;
            match = m;
            replacement = r;
        }
    }

    class LogResults // Класс для сохранения информации о проделанных над текстом действиях.
    {

        public PreparedDictElement OriginalMatchElement; // Ссылка на оригинальный элемент, который соответствует замене.
        public string FoundMatch;
        public string CorrespondedSubstitution;
        public string FoundMatch_extended; // Если подпоследовательность найдена в середине текста и нужен дополнительный контекст слева и справа от неё.
        public string CorrespondedSubstitution_extended;
        public int lineNumber = -1; // В случае замены эти координаты соответствуют началу заменённой строки (могут сбиваться, т.к. изменения вносятся поэтапно). Если же алгоритм не меняет файлы, то эти значени яуказывают на местоположение оригинального совпадения.
        public int columnNumber = -1;

        public LogResults() { }

        public LogResults(PreparedDictElement dict_element, Match match, string input, string output, string replacement, int changed_char_index, int line, int extended_symbols)
        {
            OriginalMatchElement = dict_element;
            FoundMatch = match.ToString();
            CorrespondedSubstitution = replacement;
            int extended_start;
            int extended_length;
            string addition_left = "...";
            string addition_right = "...";

            extended_start = match.Index - extended_symbols;
            extended_length = FoundMatch.Length + extended_symbols * 2;
            if (extended_start < 0)
            {
                addition_left = "";
                extended_length += extended_start;
                extended_start = 0;
            }
            int input_length = input.Length;
            if (extended_start + extended_length > input_length)
            {
                addition_right = "";
                extended_length = input_length - extended_start;
            }

            FoundMatch_extended = addition_left + input.Substring(extended_start, extended_length) + addition_right;

            extended_start = changed_char_index - extended_symbols;
            extended_length = FoundMatch.Length + extended_symbols * 2;
            if (extended_start < 0)
            {
                extended_length += extended_start;
                extended_start = 0;
            }
            int output_length = output.Length;
            if (extended_start + extended_length > output_length)
            {
                extended_length = output_length - extended_start;
            }
            CorrespondedSubstitution_extended = addition_left + output.Substring(extended_start, extended_length) + addition_right;

            columnNumber = line;
        }
    }

    class ReplaceResults
    {
        public List<LogResults> Logs;
        public List<LogResults> AdditionLogs; // Для разделения на те слова которые есть в Разговорнике, и те, которых нету.
        public string output;
    }

    class ObjectSubstitutionInfo
    {
        public string displayed_text = "";
        public int type; // 0 = имя файла/папки, 1 = объект на сцене, 2 = текст.
        public string guid; // Для ассетов.
        public GlobalObjectId gameObjectID; // Для объектов.

        public List<ObjectSubstitutionInfo> Childrens = new List<ObjectSubstitutionInfo>();
        public List<LogResults> Details = new List<LogResults>();

        public ObjectSubstitutionInfo(UnityEngine.Object obj = null, int _type = 2)
        {
            type = _type;

            switch (type)
            {
                case 0:
                    guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
                    break;
                case 1:
                    gameObjectID = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                    break;
            }
        }
    }

    List<ObjectSubstitutionInfo> ProvidedSubstitutions; // Список предусмотренных замен (зафиксированы пользователем в Разговорнике).
    List<ObjectSubstitutionInfo> AdditionSubstitutions; // Список непредусмотренных замен (обнаруженные последовательности русских символов, не покрытые Разговорником).



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
                Match prev_match = MatchesLogInfos[i-1].match;
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


    ReplaceResults translit_text(string input) // Простой транслит
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


    string MaintainLettersCase(string original, string replacement) // Автоматическое сопостовление регистров совпадения и замены если LetterCase = false (т.е. если поиск замен не зависит от регистра, ведь нам желательно сохранить формат).
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
                case 1:
                    original_i = Math.Min(i, PascalsOriginal.Count - 1); // Если заменяемая последовательность делится при помощи PascalCase на большее количество слов чем в оригинальной строке, то последние слова заменяемой последовательности форматируются в соответствии с форматом последнего слова из оригинала.
                    break;
                case 2:
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


    string CollectMatchesAndReplace(Match match, List<LastMatchesInfo> _nowProcessedMatches, PreparedDictElement _nowProcessedDictEl)
    {
        string replace = match.Result(_nowProcessedDictEl.Regex_eng);
        if (!current_settings.LetterCase) replace = MaintainLettersCase(match.Value, replace);
        LastMatchesInfo match_info = new LastMatchesInfo(_nowProcessedDictEl, match, replace);
        _nowProcessedMatches.Add(match_info);
        return replace;
    }

    MatchEvaluator MakeProcessMatchesEvaulator(List<LastMatchesInfo> _nowProcessedMatches, PreparedDictElement _nowProcessedDictEl) // _nowProcessedMatches и _nowProcessedDictEl обязаны быть "локальными", чтобы код был чётким и последовательным, а ещё так будет потенциал для распараллеливания.
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


    string CollectAdditionalWordsAndReplace(Match match, List<LastMatchesInfo> _nowProcessedMatches)
    {
        string rus = match.Value;
        if (!current_settings.LetterCase) rus = rus.ToLower();
        RenameSettings.DictElement CoresspondedWord;

        if (AutoFoundWords.ContainsKey(rus))
        {
            CoresspondedWord = AutoFoundWords[rus];
        }
        else
        {
            string eng = "";
            switch (current_settings.user_type)
            {
                case 1: // Просто Разговорник (без доп-замен, но с поиском слов).
                    eng = match.Value; // Дублирую слово в колонку разговорника, на случай если пользователь не захочет его заменять.
                    break;
                case 3: // Разговорник + переводчик
                    //eng = Получить_из_переводчика(match.Value);
                    //eng = MaintainLettersCase(match.Value, eng);
                    //eng = eng.Replace(" ", ""); // Так как в любом случае в найденной оригинальной строке не было пробелов, а гугл мог их добавить.
                    //if (ReplaceDashes) eng = eng.Replace("-", "_");
                    break;
            }

            CoresspondedWord = new RenameSettings.DictElement(rus, eng);
            AutoFoundWords.Add(rus, CoresspondedWord);
            AutoRazgovornik.Add(CoresspondedWord);
        }

        string replace;
        if (current_settings.user_type == 1)
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

    MatchEvaluator MakeProcessAdditionalWordsEvaulator(List<LastMatchesInfo> _nowProcessedMatches)
    {
        return new MatchEvaluator((Match match) => CollectAdditionalWordsAndReplace(match, _nowProcessedMatches));
    }


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


    ReplaceResults process_text(string text)
    {
        ReplaceResults results = new ReplaceResults();
        results.Logs = new List<LogResults>();
        results.AdditionLogs = new List<LogResults>();

        ReplaceResults partitial_res;

        if (current_settings.user_type == 0) // Режим транслита
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


            if (current_settings.user_type == 2) // Разговорник + транслит
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

    private class ObjectsSubstitutionResults
    {
        public List<ObjectSubstitutionInfo> main_info = new List<ObjectSubstitutionInfo>(); // Соответствует ProvidedSubstitutions.
        public List<ObjectSubstitutionInfo> addition_info = new List<ObjectSubstitutionInfo>(); // Соответствует AdditionSubstitution.
    }


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


    ObjectsSubstitutionResults process_gameobjects(GameObject[] objects_to_process)
    {
        ObjectsSubstitutionResults result = new ObjectsSubstitutionResults();

        foreach(GameObject obj in objects_to_process)
        {
            ObjectSubstitutionInfo obj_main_info = new ObjectSubstitutionInfo(obj, 1);
            ObjectSubstitutionInfo obj_addition_info = new ObjectSubstitutionInfo(obj, 1);
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


    List<UnityEngine.Object> GetFolderContents(string folder_path) // Возвращает все ассеты внутри папки (файлы и папки, учтённые в AssetDatabase) без рекурисвого просмотра подпапок.
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
            string new_name="";

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

    ObjectsSubstitutionResults StartProcess()
    {
        ObjectsSubstitutionResults results = process_assets(root_settings_window.selected);
        ProvidedSubstitutions = results.main_info;
        AdditionSubstitutions = results.addition_info;
        Debug.Log("ПРОЦЕСС ПЕРЕТОЛМАЧИВАТЕЛЯ ЗАВЕРШОН.");
        return results;
    }

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

    public RenameActor(RenameSettings root, bool _make_changes = false)
    {
        root_settings_window = root;
        make_changes = _make_changes;
        current_settings = root.settings.Clone(); // копируем настройки.

        RenameSettings.AutoSortedDict Razgovornik = root.Razgovornik;
        int current_SortRazgovornik = Razgovornik.SortRazgovornik;

        switch (current_settings.AutoFindWordsType)
        {
            case 0:
                AutoFindRegex = NativePascalWordRegex;
                break;
            case 1:
                AutoFindRegex = NativeWordRegex;
                break;
            case 2: // Один разделяющий символ
                AutoFindRegex = new Regex(NativeWord + @"(?n)(." + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                break;
            case 3: // Два разделяющих символа
                AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,2}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                break;
            case 4: // Три разделяющих символа
                AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,3}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                break;
            case 5: // Четыре разделяющих символа
                AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,4}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                break;
            case 6: // Пять разделяющих символов
                AutoFindRegex = new Regex(NativeWord + @"(?n)(.{1,5}" + NativeWord + @")+(?-n)", RegexOptions.Compiled);
                break;
            case 7: // Предложение
                AutoFindRegex = new Regex("[А-ЯЁ][а-яё\\s,;:—\\-\\\"]*([;]|[.?!]{0,3}|(?=[A-Za-z]|$))", RegexOptions.Compiled);
                break;
            case 8: // Другое
                AutoFindRegex = new Regex(current_settings.CustomFindRegex);
                break;
        }

        if (current_settings.ActuallySortByLength)
        {
            Razgovornik.SortRazgovornik = 2; // Сортирую по длине. Причины:
                                             //     1) На случай если кому-то понадобится заменять наименования вроде "весёлыйгусь", то можно было бы разрешить вхождения подслов, и при этом всё равно избежать неправильных замен.
                                             //     2) Чтобы учесть и вторую колонку в том числе, для однозначности и определённости, чтобы алгоритм не брал случайное из значений, если ключи в левой колонке Разговорника по недосмотру пользователя совпадут.
        }

        foreach (RenameSettings.DictElement i in Razgovornik)
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

            if (!current_settings.LetterCase && !current_settings.UseRegularExpr && !current_settings.SubwordsInside && current_settings.AutoFindWordsType == 0) // Если PascalCase и отключен учёт регистра, проще просто обработать этот случай отдельно.
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
                else rus = rus+ @"(?-i)(?-n)";

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

        Razgovornik.SortRazgovornik = current_SortRazgovornik;

        StartProcess();
        ShowResults();
    }


    // ГРАФИЧЕСКАЯ РЕПРЕЗЕНТАЦИЯ РЕЗУЛЬТАТОВ:
    class ResultsMainWindow : EditorWindow
    {
        RenameActor actor;
        RenameSettings root_settings_window;
        static Color obvodka_color = new Color(0, 0, 0, 1);

        class objectLogsRepresentation : VisualElement
        {
            public ObjectSubstitutionInfo info;
            const string expanded_icon = "d_in_foldout_on";
            const string collapsed_icon = "d_in_foldout";
            objectLogsRepresentation parentLog;
            public int depth; // Глубина вложенности, нужна для вычисления отступа.
            const float depth_gap = 15; // Размер отступа на единицу глубины.

            public VisualElement Make_foldout_header_contents()
            {
                VisualElement foldout_header_contents = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, } };


                Texture icon;
                switch (info.type) // Иконка объекта
                {
                    case 1:
                        icon = AssetPreview.GetMiniTypeThumbnail(typeof(GameObject));
                        break;
                    case 2:
                        icon = EditorGUIUtility.IconContent("TextMesh Icon").image;
                        break;
                    default:
                        icon = AssetPreview.GetMiniThumbnail(GetAssetToFocus());//EditorGUIUtility.IconContent("d_eyeDropper.sml").image; // дефолтная иконка
                        break;
                }

                foldout_header_contents.Add(new Image()
                {
                    image = icon,
                    scaleMode = ScaleMode.ScaleToFit,
                    style = {
                        flexShrink = 0,
                        width = 15,
                        height = 15,
                    }
                });

                Label displayed = new Label() // Лейбл
                {
                    text = info.displayed_text,
                    style = {
                        unityTextAlign = TextAnchor.MiddleLeft,
                    }
                };


                foldout_header_contents.Add(displayed);

                if (info.Details.Count > 0)
                {
                    displayed.style.unityFontStyleAndWeight = FontStyle.Italic;
                    displayed.text += " (Вхождений: " + info.Details.Count.ToString() + ")";
                }
                else displayed.style.unityFontStyleAndWeight = FontStyle.Bold;

                return foldout_header_contents;
            }

            Image paddle_triange = new Image()
            {

                image = EditorGUIUtility.IconContent(collapsed_icon).image,
                style =
                {
                    width = 15,
                    height = 15,
                    flexGrow = 0
                }
            };
            VisualElement ContentsList = new VisualElement();

            private bool _expended;
            bool expended
            {
                get => _expended;
                set
                {
                    _expended = value;
                    if (value)
                    {
                        paddle_triange.image = EditorGUIUtility.IconContent(expanded_icon).image;
                        ContentsList.style.display = DisplayStyle.Flex;
                        ContentsList.SetEnabled(true);
                    }
                    else
                    {
                        paddle_triange.image = EditorGUIUtility.IconContent(collapsed_icon).image;
                        ContentsList.style.display = DisplayStyle.None;
                        ContentsList.SetEnabled(false);
                    }
                }
            }

            public objectLogsRepresentation(ObjectSubstitutionInfo _info, objectLogsRepresentation _parent = null)
            {
                //style.display = DisplayStyle.Flex;
                //style.flexDirection = FlexDirection.Row;
                //style.flexGrow = 1;

                info = _info;
                parentLog = _parent;
                if (parentLog != null) depth = parentLog.depth + 1;
                else depth = 0;

                VisualElement foldout_header = new VisualElement()
                {
                    style = {
                        alignItems = Align.Center,
                        flexDirection = FlexDirection.Row
                    }
                };

                foldout_header.Add(new VisualElement() { style = { width = depth_gap * depth, flexShrink = 0 } });

                // Треугольник (кнопка раскрыть-закрыть список):
                if (info.Childrens.Count > 0)
                {
                    paddle_triange.style.maxHeight = paddle_triange.style.minHeight;
                    Clickable foldin_foldout_clickable = new Clickable(() => { expended = !expended; });
                    paddle_triange.AddManipulator(foldin_foldout_clickable);
                    foldout_header.Add(paddle_triange);
                }
                else
                {
                    // пустое пространство:
                    foldout_header.Add(new VisualElement() { style = { height = paddle_triange.style.height, width = paddle_triange.style.width } });
                }

                // Содержание заголовка:
                VisualElement foldout_header_contents = Make_foldout_header_contents();

                Clickable focus = new Clickable(FocusOnObject);
                focus.activators.Clear(); // Оно по умолчанию создаёт фильтр, который считывает любое нажатие левой кнопки мыши, приходится удалять вручную.
                focus.activators.Add(new ManipulatorActivationFilter { clickCount = 2, button = MouseButton.LeftMouse });
                foldout_header_contents.AddManipulator(focus);

                GenericMenu RightClickMenu = new GenericMenu();
                Clickable doubleclickmenu = new Clickable(RightClickMenu.ShowAsContext);
                doubleclickmenu.activators.Clear(); // Оно по умолчанию создаёт фильтр, который считывает любое нажатие левой кнопки мыши, приходится удалять вручную.
                doubleclickmenu.activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
                foldout_header.AddManipulator(doubleclickmenu);


                if (info.Details.Count > 0)
                {
                    RightClickMenu.AddItem(new GUIContent("Показать подробности"), false, ShowInfo);
                }
                else
                    RightClickMenu.AddDisabledItem(new GUIContent("Показать подробности (нет вхождений)"), false);

                foldout_header.Add(foldout_header_contents);

                foreach (ObjectSubstitutionInfo child in info.Childrens)
                {
                    ContentsList.Add(new objectLogsRepresentation(child, this));
                }

                style.flexDirection = FlexDirection.Column;
                Add(foldout_header);
                Add(ContentsList);

                expended = false;
            }

            public UnityEngine.Object GetAssetToFocus()
            {
                switch (info.type)
                {
                    case 0:
                        return AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(info.guid));
                    default:
                        return parentLog.GetAssetToFocus();
                }
            }

            public void FocusOnObject()
            {
                UnityEngine.Object ass_to_focus = GetAssetToFocus();

                switch (info.type)
                {
                    case 0:
                        Selection.SetActiveObjectWithContext(ass_to_focus, null);
                        break;
                    case 2:
                        if (!AssetDatabase.OpenAsset(ass_to_focus, 0, 0)) Debug.LogError("Не удалось открыть файл " + AssetDatabase.GetAssetPath(ass_to_focus));
                        break;
                    case 1:
                        Type t = ass_to_focus.GetType();
                        if (t == typeof(SceneAsset))
                        {
                            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(ass_to_focus), OpenSceneMode.Single);
                            Selection.activeGameObject = (GameObject)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(info.gameObjectID);
                        }
                        if (t == typeof(GameObject))
                        {
                            AssetDatabase.OpenAsset(ass_to_focus);
                        }
                        SceneView.lastActiveSceneView.FrameSelected();
                        break;
                }
            }

            void ShowInfo()
            {
                DetailsWindow window = EditorWindow.CreateWindow<DetailsWindow>(info.displayed_text, desiredDockNextTo: new Type[]
                {
                    typeof(ResultsMainWindow)
                });
                window.CreateGGUI_delayed(this);
                window.Show();
            }
        }

        class DetailsWindow : EditorWindow
        {
            const int gap = 6;
            const int gap_smaller = 3;
            static Color outline_color = Color.white;

            class InfoBlock : VisualElement
            {
                class NumberLine : TextElement
                {
                    private string _fieldName;
                    public int num
                    {
                        set
                        {
                            text = _fieldName + ": " + value.ToString();

                        }
                    }

                    public NumberLine(string FieldName, int n)
                    {
                        _fieldName = FieldName;
                        num = n;

                        style.borderTopWidth = gap;
                        style.borderBottomWidth = gap;
                        style.borderRightWidth = gap;
                        style.borderLeftWidth = gap;
                    }
                }

                class SomeTextBox : VisualElement
                {
                    private TextElement info_field;
                    public string text
                    {
                        get => info_field.text;

                        set
                        {
                            info_field.text = value;
                        }
                    }

                    public SomeTextBox(string FieldName, string text)
                    {
                        style.borderTopWidth = gap;
                        style.borderBottomWidth = gap;
                        style.borderRightWidth = gap;
                        style.borderLeftWidth = gap;

                        TextElement label = new TextElement()
                        {
                            text = FieldName + ": ",
                            style =
                            {
                                width = 100
                            }
                        };

                        Box obvodka = new Box()
                        {
                            style =
                        {
                            flexGrow = 1,
                            alignSelf = Align.Stretch,
                            //backgroundColor = new Color(42, 42, 42, 1),
                            borderTopColor = outline_color,
                            borderBottomColor = outline_color,
                            borderLeftColor = outline_color,
                            borderRightColor = outline_color,
                            borderTopWidth = 1,
                            borderBottomWidth = 1,
                            borderLeftWidth = 1,
                            borderRightWidth = 1,
                        }
                        };
                        ScrollView scroll_view = new ScrollView() { style = { height = 50, } };
                        info_field = new TextElement()
                        {
                            text = text,
                            style = {
                            borderTopWidth = gap_smaller,
                            borderBottomWidth = gap_smaller,
                            borderRightWidth = gap_smaller,
                            borderLeftWidth = gap_smaller,
                        }
                        };
                        scroll_view.Add(info_field);
                        obvodka.Add(scroll_view);

                        style.flexDirection = FlexDirection.Row;
                        Add(label);
                        Add(obvodka);
                    }
                }

                NumberLine StepLine;
                SomeTextBox FoundMatch;
                SomeTextBox CorrespondedSubstitution;
                SomeTextBox original_rus;
                SomeTextBox original_eng;
                SomeTextBox FoundMatch_extended;
                SomeTextBox CorrespondedSubstitution_extended;
                NumberLine _lineNumber;
                NumberLine _columnNumber;
                int line;
                int column;

                public InfoBlock(LogResults exemple_info, objectLogsRepresentation parent_block)
                {
                    VisualElement vis_el = new VisualElement() { style = { borderTopWidth = gap, borderBottomWidth = gap, borderRightWidth = gap, borderLeftWidth = gap} };

                    Box obvodka = new Box()
                    {
                        style =
                    {
                        //backgroundColor = new Color(42, 42, 42, 1),
                        borderTopColor = outline_color,
                        borderBottomColor = outline_color,
                        borderLeftColor = outline_color,
                        borderRightColor = outline_color,
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderLeftWidth = 1,
                        borderRightWidth = 1,
                    }
                    };

                    StepLine = new NumberLine("Шаг", 0);
                    obvodka.Add(StepLine);
                    FoundMatch = new SomeTextBox("Вхождение", exemple_info.FoundMatch);
                    obvodka.Add(FoundMatch);
                    if (exemple_info.OriginalMatchElement != null)
                    {
                        CorrespondedSubstitution =  new SomeTextBox("Замена", exemple_info.CorrespondedSubstitution);
                        obvodka.Add(CorrespondedSubstitution);
                        original_rus = new SomeTextBox("Оригинальный шаблон вхождения", exemple_info.OriginalMatchElement.original_rus);
                        obvodka.Add(original_rus);
                        original_eng = new SomeTextBox("Оригинальный шаблон замены", exemple_info.OriginalMatchElement.original_eng);
                        obvodka.Add(original_eng);
                    }
                    FoundMatch_extended = new SomeTextBox("Контекст вхождения", exemple_info.FoundMatch_extended);
                    obvodka.Add(FoundMatch_extended);
                    if (exemple_info.OriginalMatchElement != null)
                    {
                        CorrespondedSubstitution_extended = new SomeTextBox("Контекст замены", exemple_info.CorrespondedSubstitution_extended);
                        obvodka.Add(CorrespondedSubstitution_extended);
                    }
                    if (exemple_info.lineNumber > -1)
                    {
                        line = exemple_info.lineNumber + 1;
                        _lineNumber = new NumberLine("Строка", line);
                        obvodka.Add(_lineNumber);
                    }
                    else line = 1;
                    column = exemple_info.columnNumber + 1;
                    _columnNumber = new NumberLine("Символ", column);
                    obvodka.Add(_columnNumber);
                    vis_el.Add(obvodka);
                    Add(vis_el);

                    Clickable FocusOnSubstitution = new Clickable(() =>
                    {
                        if (parent_block.info.type == 2)
                        {
                            UnityEngine.Object ass_to_focus = parent_block.GetAssetToFocus();
                            if (!AssetDatabase.OpenAsset(ass_to_focus, line, column)) Debug.LogError("Не удалось открыть файл " + ass_to_focus);
                        }
                        else parent_block.FocusOnObject();
                    });
                    FocusOnSubstitution.activators.Clear();
                    FocusOnSubstitution.activators.Add(new ManipulatorActivationFilter { clickCount = 2, button = MouseButton.LeftMouse });
                    this.AddManipulator(FocusOnSubstitution);
                }

                public void SetInfo(int step, LogResults info)
                {
                    StepLine.num = step;
                    FoundMatch.text = info.FoundMatch;

                    if (info.OriginalMatchElement != null)
                    {
                        CorrespondedSubstitution.text = info.CorrespondedSubstitution;
                        original_rus.text = info.OriginalMatchElement.original_rus;
                        original_eng.text = info.OriginalMatchElement.original_eng;
                    }

                    FoundMatch_extended.text = info.FoundMatch_extended;

                    if (info.OriginalMatchElement != null)
                    {
                        CorrespondedSubstitution_extended.text = info.CorrespondedSubstitution_extended;
                    }

                    if (info.lineNumber > -1)
                    {
                        line = info.lineNumber + 1;
                        _lineNumber.num = line;
                    }
                    else line = 1;

                    column = info.columnNumber + 1;
                    _columnNumber.num = column;

                }
            }

            public void CreateGGUI_delayed(objectLogsRepresentation root_element)
            {
                VisualElement displayed_header = root_element.Make_foldout_header_contents();
                VisualElement root = rootVisualElement;
                root.style.alignSelf = Align.Stretch;
                root.style.alignItems = Align.FlexStart;
                VisualElement vis_el = new VisualElement() { style = { flexShrink = 0, borderTopWidth = gap, borderBottomWidth = gap, borderRightWidth = gap, borderLeftWidth = gap, } };
                vis_el.Add(displayed_header);
                root.Add(vis_el);

                string type_line = "Тип: ";
                switch (root_element.info.type)
                {
                    case 0:
                        type_line += "имя объекта файловой системы";
                        break;
                    case 1:
                        type_line += "имя объекта на сцене";
                        break;
                    case 2:
                        type_line += "содержание текстового файла";
                        break;
                    default:
                        type_line += "<UNDEFINED>";
                        break;
                }
                root.Add(new TextElement() { text = type_line, style = { textOverflow = TextOverflow.Ellipsis, flexShrink = 0, borderTopWidth = gap * 2, borderBottomWidth = gap * 2, borderRightWidth = gap, borderLeftWidth = gap, } });

                Box obvodka = new Box()
                {
                    style =
                    {
                        alignSelf = Align.Stretch,
                        flexGrow = 1,
                        flexDirection = FlexDirection.Column,
                        //backgroundColor = new Color(42, 42, 42, 1),
                        borderTopColor = outline_color,
                        borderBottomColor = outline_color,
                        borderLeftColor = outline_color,
                        borderRightColor = outline_color,
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderLeftWidth = 1,
                        borderRightWidth = 1,
                    }
                };

                obvodka.Add(new VisualElement() { style = { width = 500,} }); // костыль-распорка


                InfoBlock TestInfoBlock = new InfoBlock(root_element.info.Details[0], root_element) { style = { flexShrink = 0} }; // Для замерки высоты элемента (она может варьироваться)
                root.Add(TestInfoBlock);

                void set_the_height (GeometryChangedEvent _) // Косты-ы-ы-ыль! Дикий, но симпотишный! ( ͡^ ͜ʖ ͡^)
                {
                    TestInfoBlock.UnregisterCallback<GeometryChangedEvent>(set_the_height);
                    float list_element_height = TestInfoBlock.resolvedStyle.height;
                    root.Remove(TestInfoBlock);


                    void bind_el(VisualElement el, int i)
                    {
                        ((InfoBlock)el).SetInfo(i + 1, root_element.info.Details[i]);
                    }
                    ListViewAdvanced DetailsList = new ListViewAdvanced(root_element.info.Details, (int)list_element_height, () => new InfoBlock(root_element.info.Details[0], root_element), bind_el)
                    { selectionType = SelectionType.None, style = { flexGrow = 1, flexShrink = 1, maxHeight = 2000, minHeight = 0} };
                    DetailsList.Refresh();

                    obvodka.Add(DetailsList);
                    root.Add(obvodka);
                    root.Add(new VisualElement() { style = { height = gap * 2, flexShrink = 0 } });

                }

                TestInfoBlock.RegisterCallback<GeometryChangedEvent>(set_the_height);
            }
        }

        const float labelfontsize = 15;
        const float SpaceBetweenElements = RenameSettings.SpaceBetweenElements / 2;

        class EmptyMarker : IMGUIContainer
        {
            public EmptyMarker(VisualElement ProvidedBlock)
            {
                style.alignItems = Align.Center;
                style.alignContent = Align.Center;
                style.unityTextAlign = TextAnchor.MiddleCenter;
                style.flexGrow = 1;

                Add(new TextElement()
                {
                    text = "<пусто>",
                    style =
                                {
                                    width = ProvidedBlock.style.width,
                                    height = ProvidedBlock.style.height,
                                    borderTopWidth = SpaceBetweenElements,
                                    borderBottomWidth = SpaceBetweenElements,
                                    unityTextAlign = TextAnchor.MiddleCenter,
                                    alignSelf = Align.Center,
                                    alignContent = Align.Center,
                                    alignItems = Align.Center,
                                    unityTextOverflowPosition = TextOverflowPosition.Middle,
                                    unityFontStyleAndWeight = FontStyle.Italic,
                                }
                });

                onGUIHandler = () =>
                {
                    if (ProvidedBlock.childCount > 0)
                    {
                        ProvidedBlock.style.display = DisplayStyle.Flex;
                        ProvidedBlock.SetEnabled(true);
                        this.style.display = DisplayStyle.None;
                        this.SetEnabled(false);
                    }
                    else
                    {
                        ProvidedBlock.style.display = DisplayStyle.None;
                        ProvidedBlock.SetEnabled(false);
                        this.style.display = DisplayStyle.Flex;
                        this.SetEnabled(true);
                    }
                };
            }
        }

        class LogsList : VisualElement
        {
            ScrollView ProvidedSubstitutionsBlock;

            public LogsList(string title, float ListsHeight)
            {
                TextElement label = new TextElement()
                {
                    text = title,
                    style =
                    {
                        borderTopWidth = SpaceBetweenElements,
                        borderBottomWidth = SpaceBetweenElements,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        alignSelf = Align.Center,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        fontSize = labelfontsize,
                    }
                };

                base.Add(label);

                Box obvodka = new Box()
                {
                    style =
                    {
                        alignSelf = Align.Center,

                        //backgroundColor = new Color(42, 42, 42, 1),
                        borderTopColor = obvodka_color,
                        borderBottomColor = obvodka_color,
                        borderLeftColor = obvodka_color,
                        borderRightColor = obvodka_color,
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderRightWidth = 1,
                        borderLeftWidth = 1,
                    }
                };

                ProvidedSubstitutionsBlock = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
                {
                    style =
                    {
                        height = ListsHeight, alignSelf = Align.Center, width = 400,
                    }
                };

                obvodka.Add(new EmptyMarker(ProvidedSubstitutionsBlock));

                obvodka.Add(ProvidedSubstitutionsBlock);
                base.Add(obvodka);
            }

            public new void Add(VisualElement el)
            {
                ProvidedSubstitutionsBlock.Add(el);
            }
        }

        class AdditionRazgovornikList : VisualElement
        {
            static Color color_gray = obvodka_color / 3;

            public AdditionRazgovornikList(string title, float ListsHeight, RenameSettings.AutoSortedDict ListOfWords)
            {
                TextElement label = new TextElement()
                {
                    text = title,
                    style =
                    {
                        borderTopWidth = SpaceBetweenElements,
                        borderBottomWidth = SpaceBetweenElements,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        alignSelf = Align.Center,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        fontSize = labelfontsize,
                    }
                };

                Add(label);

                Box obvodka = new Box()
                {
                    style =
                    {
                        alignSelf = Align.Center,
                        //backgroundColor = new Color(42, 42, 42, 1),
                        borderTopColor = obvodka_color,
                        borderBottomColor = obvodka_color,
                        borderLeftColor = obvodka_color,
                        borderRightColor = obvodka_color,
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderRightWidth = 1,
                        borderLeftWidth = 1,
                    }
                };

                words_pair testPair = new words_pair(ListsHeight) { style = { flexShrink = 0 } }; // Для замерки высоты элемента
                testPair.SetWords("test", "test");
                obvodka.Add(testPair);

                void set_the_height(GeometryChangedEvent _)
                {
                    obvodka.UnregisterCallback<GeometryChangedEvent>(set_the_height);
                    int list_element_height = (int)testPair.resolvedStyle.height;
                    obvodka.Remove(testPair);


                    void bind_el(VisualElement _el, int i)
                    {
                        words_pair el = (words_pair)_el;
                        el.SetWords(ListOfWords[i].rus, ListOfWords[i].eng);
                    }

                    ListView WordsList = new ListView(ListOfWords, list_element_height, () => new words_pair(250), bind_el)
                    {
                        selectionType = SelectionType.None,
                        style =
                        {
                            height = ListsHeight, alignSelf = Align.Center, width = 250, alignItems = Align.Center,
                        }
                    };

                    if (ListOfWords.Count == 0) obvodka.Add(new EmptyMarker(WordsList));
                    obvodka.Add(WordsList);
                }

                obvodka.RegisterCallback<GeometryChangedEvent>(set_the_height);
                Add(obvodka);
            }

            class words_pair : VisualElement
            {
                const int WordHeight = 20;
                public TextElement rus_word;
                public TextElement eng_word;

                public words_pair(float WordsListWidth)
                {
                    int half_width = (int)(WordsListWidth / 2);
                    Box obvodka_1 = new Box()
                    {
                        style =
                    {
                        width = half_width,
                        flexGrow = 1,
                        //backgroundColor = new Color(42, 42, 42, 1),
                        borderTopColor = color_gray,
                        borderBottomColor = color_gray,
                        borderLeftColor = color_gray,
                        borderRightColor = color_gray,
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderRightWidth = 1,
                    }
                    };

                    Box obvodka_2 = new Box()
                    {
                        style =
                    {
                        width = half_width,
                        flexGrow = 1,
                        //backgroundColor = new Color(42, 42, 42, 1),
                        borderTopColor = color_gray,
                        borderBottomColor = color_gray,
                        borderLeftColor = color_gray,
                        borderRightColor = color_gray,
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderLeftWidth = 1,
                    }
                    };

                    const int otstup = 2;
                    rus_word = new TextElement() { style = { borderTopWidth = otstup, borderBottomWidth = otstup, borderRightWidth = otstup, borderLeftWidth = otstup, } };
                    eng_word = new TextElement() { style = { borderTopWidth = otstup, borderBottomWidth = otstup, borderRightWidth = otstup, borderLeftWidth = otstup, } };
                    ScrollView scroller1 = new ScrollView(ScrollViewMode.Horizontal) { style = { height = WordHeight } };
                    ScrollView scroller2 = new ScrollView(ScrollViewMode.Horizontal) { style = { height = WordHeight } };
                    //scroller1.Add(rus_word);
                    //scroller2.Add(eng_word);
                    //obvodka_1.Add(scroller1);
                    //obvodka_2.Add(scroller2);
                    obvodka_1.Add(rus_word);
                    obvodka_2.Add(eng_word);
                    VisualElement Row = new VisualElement() { style = { flexDirection = FlexDirection.Row, width = WordsListWidth, flexGrow = 1 } };
                    Row.Add(obvodka_1);
                    Row.Add(obvodka_2);
                    Add(Row);
                }

                public void SetWords(string word_1, string word_2)
                {
                    rus_word.text = word_1;
                    eng_word.text = word_2;
                }
            }
        }

        public void SetData(RenameActor _actor)
        {
            actor = _actor;
            root_settings_window = actor.root_settings_window;
            CreateGUI_delayed();
        }

        public void CreateGUI_delayed() // Вместо обычного CreateGUI, просто потому что Unity не даёт мне одновременно передать при создании необходимын для окна данные и в то же время прикрепить вкладку к окну-источнику (а прикрепить, по видимому, можно только при создании.)
        {
            ScrollView MainScrollArea = new ScrollView();
            //selectedOption = EditorGUILayout.Popup("Select an option:", selectedOption, options);
            float ListsHeight = RenameSettings.ListsHeight;


            string label_1="";
            string label_2="";

            switch (actor.current_settings.user_type)
            {
                case 0:
                    label_1 = "Транслит:";
                    break;
                case 1:
                    label_1 = "Разговорник:";
                    label_2 = "Остальные вхождения (автопоиск):";
                    break;
                case 2:
                    label_1 = "Разговорник:";
                    label_2 = "Транслит:";
                    break;
                case 3:
                    label_1 = "Разговорник:";
                    label_2 = "Автоперевод:";
                    break;
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
            if (actor.current_settings.user_type == 1 || actor.current_settings.user_type == 3)
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
                    foreach (RenameSettings.DictElement word in actor.AutoRazgovornik)
                    {
                        root_settings_window.Razgovornik.Add(new RenameSettings.DictElement(word.rus, word.eng));
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
