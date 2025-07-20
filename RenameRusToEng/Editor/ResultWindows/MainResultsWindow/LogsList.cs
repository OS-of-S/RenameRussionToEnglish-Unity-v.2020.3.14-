using UnityEngine;
using UnityEngine.UIElements;
using SmectUI;

namespace RenameRusToEng
{

    /// <summary>
    /// UI-элемент, репрезентирующий список затронутых алгоритмом объектов в окне результатов работы Перептолмачивателя.
    /// </summary>
    class LogsList : VisualElement
    {
        static Color obvodka_color = ResultsMainWindow.obvodka_color;
        const float SpaceBetweenElements = ResultsMainWindow.SpaceBetweenElements;
        const float labelfontsize = ResultsMainWindow.labelfontsize;
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
}