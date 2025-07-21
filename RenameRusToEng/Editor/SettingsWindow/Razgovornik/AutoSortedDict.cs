using System;
using System.Collections.Generic;

namespace RenameRusToEng
{

    /// <summary>
    /// Класс Разговорника (поддерживает разные сортировки и быстрое добавление элементов).
    /// </summary>
    public class AutoSortedDict : List<DictElement>
    {
        public enum SortType
        {
            NONE,
            ALPHABETICAL,
            BY_LENGHT,
        }

        private int order_counter = 0;

        private IComparer<DictElement> ActualComparer = new DictOrderComparator();
        private AutoSortedDict.SortType _sortRazgovornik;
        public AutoSortedDict.SortType SortRazgovornik
        {
            get => _sortRazgovornik;
            set
            {
                if (value != _sortRazgovornik)
                {
                    _sortRazgovornik = value;
                    switch (value)
                    {
                        case SortType.NONE:
                            ActualComparer = new DictOrderComparator();
                            break;
                        case SortType.ALPHABETICAL:
                            ActualComparer = new DictAlphabetComparator();
                            break;
                        case SortType.BY_LENGHT:
                            ActualComparer = new DictLengthComparator();
                            break;
                    }
                    Sort();
                }
            }
        }

        private class DictOrderComparator : IComparer<DictElement>
        {
            int IComparer<DictElement>.Compare(DictElement a, DictElement b)
            {
                return a.order - b.order;
            }
        }

        private class DictAlphabetComparator : IComparer<DictElement>
        {
            int IComparer<DictElement>.Compare(DictElement a, DictElement b)
            {
                int diff = string.Compare(a.rus, b.rus, StringComparison.CurrentCulture);
                if (diff == 0)
                {
                    diff = string.Compare(a.eng, b.eng, StringComparison.CurrentCulture);
                    if (a.eng == "" || b.eng == "") return -diff; //Отдельная обработка пустой строки чтоб все пустые строки были в конце.
                }
                else
                    if (a.rus == "" || b.rus == "") return -diff; //Отдельная обработка пустой строки чтоб все пустые строки были в конце.
                return diff;
            }
        }

        public static int CompareLengthAndAlphabet(string a, string b)
        {
            int diff = b.Length - a.Length;
            if (diff == 0)
            {
                diff = string.Compare(a, b, StringComparison.CurrentCulture); // Для однозначности дополнительно сортируем по алфавиту.
            }
            return diff;
        }

        private class DictLengthComparator : IComparer<DictElement>
        {
            int IComparer<DictElement>.Compare(DictElement a, DictElement b)
            {
                int diff = CompareLengthAndAlphabet(a.rus, b.rus);
                if (diff == 0)
                {
                    diff = CompareLengthAndAlphabet(a.eng, b.eng);
                }
                return diff;
            }
        }

        private void GeneralInit()
        {
            SortRazgovornik = 0;
        }

        public AutoSortedDict() : base() { GeneralInit(); }
        public AutoSortedDict(List<DictElement> list) : base(list) { GeneralInit(); }

        public new void Add(DictElement item)
        {
            item.order = order_counter;
            order_counter++;
            base.Add(item);
        }

        public int AddSorted(DictElement item)
        {
            item.order = order_counter;
            order_counter++;
            return PutSorted(item);
        }

        public int PutSorted(DictElement item)
        {
            int index_to_insert = BinarySearch(item, ActualComparer);
            if (index_to_insert < 0) index_to_insert = ~index_to_insert;

            Insert(index_to_insert, item);
            return index_to_insert;
        }

        public new void Sort()
        {
            Sort(ActualComparer);
        }

        public int ResortElement(int item_index)
        {
            DictElement item = this[item_index];
            RemoveAt(item_index);
            return PutSorted(item);
        }
    }
}