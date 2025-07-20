using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace RenameRusToEng
{

    public class AssetsList
    {
        const int itemHeight = 18;
        public ListView selectionList;
        public void Refresh()
        {
            selectionList.Refresh();
        }

        public AssetsList(List<UnityEngine.Object> selected, UnityEngine.Object context)
        {
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
            selectionList.onItemsChosen += items => { foreach (UnityEngine.Object item in items) { Selection.SetActiveObjectWithContext(item, context); } };
        }
    }
}