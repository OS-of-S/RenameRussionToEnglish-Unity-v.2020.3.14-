using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using SmectUI;

namespace RenameRusToEng
{

    /// <summary>
    /// Окно подробностей, представляющее результаты работы Перептолмачивателя, относящиеся к конкретному объекту из списка.
    /// </summary>
    class DetailsWindow : EditorWindow
    {
        public const int gap = 6;
        public static Color outline_color = Color.white;

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
                case ObjectSubstitutionInfo.ObjectType.FILE_NAME:
                    type_line += "имя объекта файловой системы";
                    break;
                case ObjectSubstitutionInfo.ObjectType.GAMEOBJECT:
                    type_line += "имя объекта на сцене";
                    break;
                case ObjectSubstitutionInfo.ObjectType.TEXT:
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

            obvodka.Add(new VisualElement() { style = { width = 500, } }); // костыль-распорка


            InfoBlock TestInfoBlock = new InfoBlock(root_element.info.Details[0], root_element) { style = { flexShrink = 0 } }; // Для замерки высоты элемента (она может варьироваться)
            root.Add(TestInfoBlock);

            void set_the_height(GeometryChangedEvent _) // Косты-ы-ы-ыль! Дикий, но симпотишный! ( ͡^ ͜ʖ ͡^)
                                                        // Поскольку функционал юнити не позволил мне получить точные заведомо расчитанные размеры
                                                        // элемента для выводимого на экран списка ListView, то я придумал добавлять в окно тестовый элемент
                                                        // с вот таким коллбэком на просчитывание геометрии. Как только элемент выведен на экран, коллбэк
                                                        // самоотвязывается, пробный элемент удаляется, добавляется список с теперь уже известными размерами
                                                        // элементов.
            {
                TestInfoBlock.UnregisterCallback<GeometryChangedEvent>(set_the_height);
                float list_element_height = TestInfoBlock.resolvedStyle.height;
                root.Remove(TestInfoBlock);


                void bind_el(VisualElement el, int i)
                {
                    ((InfoBlock)el).SetInfo(i + 1, root_element.info.Details[i]);
                }
                ListViewAdvanced DetailsList = new ListViewAdvanced(root_element.info.Details, (int)list_element_height, () => new InfoBlock(root_element.info.Details[0], root_element), bind_el)
                { selectionType = SelectionType.None, style = { flexGrow = 1, flexShrink = 1, maxHeight = 2000, minHeight = 0 } };
                DetailsList.Refresh();

                obvodka.Add(DetailsList);
                root.Add(obvodka);
                root.Add(new VisualElement() { style = { height = gap * 2, flexShrink = 0 } });

            }

            TestInfoBlock.RegisterCallback<GeometryChangedEvent>(set_the_height);
        }
    }
}