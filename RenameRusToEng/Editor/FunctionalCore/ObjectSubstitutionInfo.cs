using System.Collections.Generic;
using UnityEditor;

namespace RenameRusToEng
{

    /// <summary>
    /// ����� ��� ���������� �������� ���������� � ��������� ������� ��������� �������, � ��� ������ � ������� ��������� � � ����������� ��� ��� ���������. � ���������� ��� ���������� ������ ��� ����������� ������� � ���� �����������. ("������" ��� ���� ������������ ������������� �����������, �� ����������� ��������.)
    /// </summary>
    class ObjectSubstitutionInfo
    {
        public enum ObjectType
        {
            FILE_NAME, // ��� �����/�����
            GAMEOBJECT, // ������ �� �����
            TEXT, // �����
        }

        public string displayed_text = ""; // �����, ������� ����� ������������ � ���� �����������.
        public ObjectType type;
        public string guid; // ��� �������.
        public GlobalObjectId gameObjectID; // ��� ��������.

        public List<ObjectSubstitutionInfo> Childrens = new List<ObjectSubstitutionInfo>(); // ���������� � �������� ��������.
        public List<LogResults> Details = new List<LogResults>(); // ����������� � ����������� ������ ���������, ����������� � ����� ��������.

        public ObjectSubstitutionInfo(UnityEngine.Object obj = null, ObjectType _type = ObjectType.TEXT)
        {
            type = _type;

            switch (type)
            {
                case ObjectType.FILE_NAME:
                    guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
                    break;
                case ObjectType.GAMEOBJECT:
                    gameObjectID = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                    break;
            }
        }
    }
}