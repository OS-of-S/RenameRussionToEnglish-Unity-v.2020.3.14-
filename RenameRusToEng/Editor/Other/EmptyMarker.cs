using UnityEngine;
using UnityEngine.UIElements;

namespace SmectUI
{

    /// <summary>
    /// UI-элемент редактора, вывод€щий на экран заместо ProvidedBlock слово "пусто" до тех пор, пока у ProvidedBlock не возникнут дети.
    /// </summary>
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
}