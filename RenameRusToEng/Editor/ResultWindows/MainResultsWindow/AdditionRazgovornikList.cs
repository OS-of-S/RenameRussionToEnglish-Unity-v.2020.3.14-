using UnityEngine;
using UnityEngine.UIElements;
using SmectUI;

namespace RenameRusToEng
{

    /// <summary>
    /// UI-элемент, репрезентирующий список-автодополнение к Разговорнику.
    /// </summary>
    class AdditionRazgovornikList : VisualElement
    {
        static Color obvodka_color = ResultsMainWindow.obvodka_color;
        const float SpaceBetweenElements = ResultsMainWindow.SpaceBetweenElements;
        const float labelfontsize = ResultsMainWindow.labelfontsize;

        public AdditionRazgovornikList(string title, float ListsHeight, AutoSortedDict ListOfWords)
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

            WordsPair testPair = new WordsPair(ListsHeight) { style = { flexShrink = 0 } }; // Для замерки высоты элемента
            testPair.SetWords("test", "test");
            obvodka.Add(testPair);

            void set_the_height(GeometryChangedEvent _)
            {
                obvodka.UnregisterCallback<GeometryChangedEvent>(set_the_height);
                int list_element_height = (int)testPair.resolvedStyle.height;
                obvodka.Remove(testPair);


                void bind_el(VisualElement _el, int i)
                {
                    WordsPair el = (WordsPair)_el;
                    el.SetWords(ListOfWords[i].rus, ListOfWords[i].eng);
                }

                ListView WordsList = new ListView(ListOfWords, list_element_height, () => new WordsPair(250), bind_el)
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
    }
}
