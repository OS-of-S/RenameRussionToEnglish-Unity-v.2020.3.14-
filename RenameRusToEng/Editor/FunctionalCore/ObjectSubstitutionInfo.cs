using System.Collections.Generic;
using UnityEditor;

namespace RenameRusToEng
{

    /// <summary>
    /// Класс для сохранения отчётной информации о некотором цельном единичном объекте, о его связях с другими объектами и о проделанных над ним действиях. В дальнейшем эта информация служит для отображения объекта в окне результатов. ("Объект" при этом определяется разработчиком произвольно, из соображений удобства.)
    /// </summary>
    class ObjectSubstitutionInfo
    {
        public enum ObjectType
        {
            FILE_NAME, // имя файла/папки
            GAMEOBJECT, // объект на сцене
            TEXT, // текст
        }

        public string displayed_text = ""; // Лейбл, который будет отображаться в окне результатов.
        public ObjectType type;
        public string guid; // Для ассетов.
        public GlobalObjectId gameObjectID; // Для объектов.

        public List<ObjectSubstitutionInfo> Childrens = new List<ObjectSubstitutionInfo>(); // Информация о дочерних объектах.
        public List<LogResults> Details = new List<LogResults>(); // Подробности о результатах работы алгоритма, относящиеся к этому элементу.

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