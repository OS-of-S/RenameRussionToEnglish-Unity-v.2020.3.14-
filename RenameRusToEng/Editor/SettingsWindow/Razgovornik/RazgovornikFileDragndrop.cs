using UnityEditor;
using UnityEngine.UIElements;
using SmectUI;

namespace RenameRusToEng
{

    class RazgovornikFileDragndrop : FilesDragAndDropManipulator
    {
        RenameSettingsWindow root;

        public RazgovornikFileDragndrop(VisualElement root_widget, RenameSettingsWindow root_window) : base(root_widget)
        {
            root = root_window;
        }

        public override bool DragFileFilter() => DragAndDrop.objectReferences.Length == 1 && AssetDatabase.Contains(DragAndDrop.objectReferences[0]);

        public override void OnDragPerform(DragPerformEvent _)
        {
            int choose = EditorUtility.DisplayDialogComplex("�������� ������������", "�� ������ �������� ������ �� ����� � �����������, � ������ �������� �� � ��.", "��������", "������", "��������");
            switch (choose)
            {
                case 0:
                    root.LoadToRazgovornik(false, AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]));
                    break;
                case 2:
                    root.LoadToRazgovornik(true, AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]));
                    break;
                case 1:
                    break;
            }
            EndDrug();
        }
    }
}