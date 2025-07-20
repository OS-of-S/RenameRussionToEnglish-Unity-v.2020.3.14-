using UnityEditor;
using UnityEngine.UIElements;

namespace SmectUI
{

    /// <summary>
    /// Шаблон для манипуляторов, обеспечивающих драг-н-дроп файлов в GUI-элементах редактора.
    /// </summary>
    abstract class FilesDragAndDropManipulator : PointerManipulator
    {
        // https://docs.unity3d.com/6000.1/Documentation/Manual/UIE-drag-across-windows.html#:~:text=From%20the%20menu%2C%20select%20Window,from%20one%20window%20to%20another.
        // https://docs.unity3d.com/2020.1/Documentation/Manual/UIE-Events-DragAndDrop.html
        //
        // Можно это и в OnGui() проделывать, через
        // if (Event.current.type == EventType.MouseDrag) и Rect.Contains(Event.current.mousePosition),
        // но это тогда будет ЕЩЁ ЗАБОРИСТЕЕ (так что предпочтительнее обойтись этими
        // пятью функциями класса PointerManipulator.)

        bool draging_files = false;

        public FilesDragAndDropManipulator(VisualElement root)
        {
            target = root;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<DragEnterEvent>(OnDragEnter);
            target.RegisterCallback<DragLeaveEvent>(OnDragLeave);
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            target.RegisterCallback<DragPerformEvent>(OnDragPerform);
            target.RegisterCallback<DragExitedEvent>(OnDragExited);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<DragEnterEvent>(OnDragEnter);
            target.UnregisterCallback<DragLeaveEvent>(OnDragLeave);
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
            target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            target.UnregisterCallback<DragExitedEvent>(OnDragExited);
        }

        public abstract bool DragFileFilter(); // Описан в наследниках, т.к. мне нужно два разных подобных класса.

        void OnDragEnter(DragEnterEvent _)
        {
            draging_files = DragFileFilter();
            if (draging_files) target.AddToClassList("drop-area--dropping");
        }

        void OnDragLeave(DragLeaveEvent _)
        {
            EndDrug();
        }

        void OnDragUpdate(DragUpdatedEvent _)
        {
            if (draging_files) DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        }

        public abstract void OnDragPerform(DragPerformEvent _); // Описан в наследниках, т.к. мне нужно два разных подобных класса.

        void OnDragExited(DragExitedEvent _)
        {
            EndDrug();
        }

        public void EndDrug()
        {
            target.RemoveFromClassList("drop-area--dropping");
        }
    }
}