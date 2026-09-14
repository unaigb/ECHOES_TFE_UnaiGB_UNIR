using UnityEngine;

namespace Echoes.Saving
{
    // Persistencia vía PlayerPrefs (un único slot de guardado). Suficiente para el Vertical
    // Slice: no hay selección de partidas ni guardado en la nube.
    public static class SaveSystem
    {
        private const string Key = "Echoes.Save";

        public static bool HasSave() => PlayerPrefs.HasKey(Key);

        public static void Save(SaveData data)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static SaveData Load()
        {
            if (!HasSave()) return null;
            return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
        }

        public static void DeleteSave()
        {
            PlayerPrefs.DeleteKey(Key);
        }
    }
}
