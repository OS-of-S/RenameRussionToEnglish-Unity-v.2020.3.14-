using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace SmectUI
{

    /// <summary>
    /// ������������ ������ ListView, ��������� �������������� � ���������� Unity: ��� ������ RefreshSize() ��� Refresh()
    /// ������ ������������� �� ������ � ������������ � �������������� minHeight � maxHeight.
    /// </summary>
    public class ListViewAdvanced : ListView // ������������ ������ ListView, ������������� ��� ��� ��������.
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