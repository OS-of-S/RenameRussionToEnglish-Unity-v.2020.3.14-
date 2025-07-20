using System.Collections;
using System;

namespace RenameRusToEng
{

    /// <summary>
    /// Интерфейс для визуализации Разговорника, возвращающий на единицу больше элементов чем
    /// имеется в Разговорнике по факту, чтобы последний из отображаемых элементов мог использоваться для добавления
    /// новой пары rus-eng в Разговорник. (Необходимость подобного интерфейса продиктована особенностью ListView,
    /// который принимает в качестве входного параметра конкретный IList, число элементов которого строго
    /// соответствует отображаемому на экране, а писать собственный аналог ListView трудоёмко.)
    /// </summary>
    class RazgovornikInterface : IList
    {
        IList Razgovornik;

        public RazgovornikInterface(IList r) { Razgovornik = r; }

        public object this[int index] { get => null; set => Razgovornik[index] = value; }

        public bool IsFixedSize => Razgovornik.IsFixedSize;

        public bool IsReadOnly => Razgovornik.IsReadOnly;

        public int Count => Razgovornik.Count + 1;

        public bool IsSynchronized => Razgovornik.IsSynchronized;

        public object SyncRoot => Razgovornik.SyncRoot;

        public int Add(object value)
        {
            return Razgovornik.Add(value);
        }

        public void Clear()
        {
            Razgovornik.Clear();
        }

        public bool Contains(object value)
        {
            return Razgovornik.Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            Razgovornik.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return Razgovornik.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return Razgovornik.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            Razgovornik.Insert(index, value);
        }

        public void Remove(object value)
        {
            Razgovornik.Remove(value);
        }

        public void RemoveAt(int index)
        {
            Razgovornik.RemoveAt(index);
        }
    }
}