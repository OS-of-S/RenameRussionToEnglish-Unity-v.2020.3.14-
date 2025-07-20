using UnityEditor;
using UnityEngine.UIElements;
using SmectUI;

namespace RenameRusToEng
{

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
}