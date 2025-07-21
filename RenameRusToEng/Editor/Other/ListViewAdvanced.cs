using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace SmectUI
{

    /// <summary>
    /// Исправленная верси¤ ListView, корректно отображающа¤с¤ в инспекторе Unity: при вызове RefreshSize() или Refresh()
    /// список раст¤гиваетс¤ по высоте в соответствии с установленными minHeight и maxHeight.
    /// </summary>
    public class ListViewAdvanced : ListView // »справленна¤ верси¤ ListView, раст¤гивающа¤ его как положено.
    {
        public ListViewAdvanced(IList itemsSource, int itemHeight, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem) : base(itemsSource, itemHeight, makeItem, bindItem) { }

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
}