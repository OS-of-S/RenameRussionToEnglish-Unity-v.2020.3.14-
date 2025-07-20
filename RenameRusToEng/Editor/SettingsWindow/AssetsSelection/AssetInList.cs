using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RenameRusToEng
{

    /// <summary>
    /// Элемент GUI, отображающий ассет в списке выбранных ассетов.
    /// </summary>
    class AssetInList : TemplateContainer
    {
        public int index;

        public AssetInList()
        {
            style.flexDirection = FlexDirection.Row;
            Add(new Image() { scaleMode = ScaleMode.ScaleToFit, style = { width = 15, height = 15, alignContent = Align.Center, flexShrink = 0 } });
            Add(new Label() { style = { flexShrink = 1 } });
            Button delete_button = new Button() { style = { flexShrink = 0 } };
            delete_button.Add(new Image()
            {
                image = EditorGUIUtility.IconContent("d_TreeEditor.Trash").image,
                scaleMode = ScaleMode.ScaleToFit,
                style = {
                width = 15,
                height = 15,
                alignContent = Align.Center,
                alignSelf = Align.FlexEnd,
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
}