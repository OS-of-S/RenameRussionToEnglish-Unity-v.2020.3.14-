using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

namespace RenameRusToEng
{

    /// <summary>
    /// Элемент GUI, представляющий отдельный объект в окне результатов.
    /// </summary>
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
                case ObjectSubstitutionInfo.ObjectType.GAMEOBJECT:
                    icon = AssetPreview.GetMiniTypeThumbnail(typeof(GameObject));
                    break;
                case ObjectSubstitutionInfo.ObjectType.TEXT:
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
                case ObjectSubstitutionInfo.ObjectType.FILE_NAME:
                    Selection.SetActiveObjectWithContext(ass_to_focus, null);
                    break;
                case ObjectSubstitutionInfo.ObjectType.TEXT:
                    if (!AssetDatabase.OpenAsset(ass_to_focus, 0, 0)) Debug.LogError("Не удалось открыть файл " + AssetDatabase.GetAssetPath(ass_to_focus));
                    break;
                case ObjectSubstitutionInfo.ObjectType.GAMEOBJECT:
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
}