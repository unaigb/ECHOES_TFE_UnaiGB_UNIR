using System;
using System.Collections;
using UnityEngine;
using Echoes.Managers;
using Echoes.Menus;
using Echoes.Saving;

namespace Echoes.Audio
{
    // Singleton de escena (igual que LevelManager, no sobrevive a un cambio de escena): reparte
    // los efectos de sonido de puertas/botones/ecos/detección/pasos y gestiona la música
    // ambient, que cambia sola con la sala (mismo evento LevelManager.OnRoomChanged que ya usan
    // el guardado y el indicador de sala). El volumen de Música/SFX se lee de los mismos
    // PlayerPrefs que ya guarda OptionsMenu — no hacía falta nada nuevo ahí.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Música por sala")]
        [SerializeField] private AudioSource musicSource;
        [Tooltip("Se usa cuando la sala actual no tiene una entrada propia en Room Musics — déjalo puesto y no hace falta rellenar Room Musics en absoluto: toda la partida suena con esta única pista, sin cortes ni reinicios entre salas (ver HandleRoomChanged). Úsalo para \"una música por estancia/escena\"; usa Room Musics solo si de verdad quieres pistas distintas por sala.")]
        [SerializeField] private AudioClip defaultMusic;
        [Tooltip("Opcional. Uno por RoomBoundary.RoomName exacto (p. ej. \"Hall 01-A\"/\"Hall 01-B\" si una sala tiene sub-zonas) — solo hace falta si quieres una pista DISTINTA en esa sala concreta; lo que no esté aquí cae en Default Music.")]
        [SerializeField] private RoomMusic[] roomMusics;
        [SerializeField] private float musicCrossfadeTime = 1.5f;
        [Tooltip("Tope real de la música: el slider de Opciones al 100% suena a este volumen, no al del clip tal cual. Por ejemplo, 0.5 = el 100% del slider sonará a la mitad del volumen original del clip.")]
        [Range(0f, 1f)] [SerializeField] private float maxMusicVolume = 0.5f;

        [Header("Efectos de sonido")]
        [SerializeField] private AudioSource sfxSource;

        [Header("Sonido posicional (proximidad)")]
        [Tooltip("Distancia (unidades de mundo) por debajo de la cual un sonido posicional se oye a volumen completo.")]
        [SerializeField] private float sfxMinDistance = 3f;
        [Tooltip("Distancia a partir de la cual un sonido posicional deja de oírse del todo.")]
        [SerializeField] private float sfxMaxDistance = 12f;

        [Serializable]
        public class RoomMusic
        {
            public string roomName;
            public AudioClip clip;
        }

        private Coroutine _musicRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (musicSource != null)
            {
                musicSource.loop = true;
                musicSource.volume = MusicVolume;
            }
        }

        private void Start()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnRoomChanged += HandleRoomChanged;
            // La música del primer momento NO arranca aquí a propósito: GameEntryPoint llama a
            // BeginRoomMusic() justo cuando termina el fundido de revelado (aviso o reinicio),
            // para que la música no se oiga sonando por debajo de la pantalla en negro.
        }

        // Llamado por GameEntryPoint en el instante exacto en que se revela la partida (arranca
        // la cinemática, o el snap de Continue). A volumen completo desde el primer frame, SIN
        // el fundido de entrada de HandleRoomChanged — ese fundido tarda ~medio segundo en subir
        // desde silencio, y aquí se pidió que sonara exactamente a la vez que la cinemática, no
        // un pelín después.
        public void BeginRoomMusic()
        {
            if (LevelManager.Instance == null || string.IsNullOrEmpty(LevelManager.Instance.CurrentRoomName)) return;

            AudioClip clip = ResolveClipForRoom(LevelManager.Instance.CurrentRoomName);
            if (clip == null) return;

            if (_musicRoutine != null) { StopCoroutine(_musicRoutine); _musicRoutine = null; }
            musicSource.clip = clip;
            musicSource.volume = MusicVolume;
            musicSource.Play();
        }

        private void OnDisable()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnRoomChanged -= HandleRoomChanged;
        }

        private float MusicVolume => PlayerPrefs.GetFloat(OptionsMenu.MusicVolumeKey, 1f) * maxMusicVolume;
        private float SfxVolume => PlayerPrefs.GetFloat(OptionsMenu.SfxVolumeKey, 1f);

        // OptionsMenu llama a esto al mover el slider (con el valor 0-1 SIN escalar), para que el
        // cambio se note ya mismo en la música que está sonando — el SFX no hace falta, PlaySfx
        // lee el volumen en el momento.
        public void SetMusicVolume(float sliderValue)
        {
            if (musicSource != null && _musicRoutine == null)
                musicSource.volume = sliderValue * maxMusicVolume;
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null || GameFlow.LeavingScene) return;
            sfxSource.PlayOneShot(clip, SfxVolume * volumeScale);
        }

        // Para sonidos del mundo (puertas, botones, placas...): más nítido cerca, se pierde con
        // la distancia. Distancia calculada a mano en X/Y (no con el sistema 3D nativo de Unity):
        // ese sistema traía dos problemas de raíz — contaba también la distancia en Z de la
        // cámara (que en un 2D con la cámara muy alejada del plano de juego dejaba todo a
        // volumen ~0) y paneaba el sonido a un lado según la dirección, cosa que no se pidió y
        // que encima con spread al máximo (el "apaño" para quitarlo) invertía los lados en vez de
        // centrarlo — un comportamiento raro del propio motor de audio 3D de Unity con spread,
        // no algo que se pueda arreglar por parámetros. Con spatialBlend=0 el sonido nunca panea
        // (sale igual por ambos altavoces) y el volumen por distancia se calcula aquí mismo.
        public void PlaySfxAt(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            // El guard de LeavingScene es el que evita el "blip" de sonido de fracción de
            // segundo cuando algo (p. ej. una caja sobre una placa) dispara su trigger de salida
            // porque Unity está destruyendo la escena de camino a otra (Main Menu, Restart Room,
            // detección) — sin esto, ese sonido se oía aunque la pantalla ya estuviera en negro.
            if (clip == null || GameFlow.LeavingScene) return;

            Vector3 listenerPos = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform.position : position;
            float distance = Vector2.Distance(position, listenerPos);
            float attenuation = 1f - Mathf.InverseLerp(sfxMinDistance, sfxMaxDistance, distance);
            if (attenuation <= 0f) return; // fuera de rango del todo, ni merece la pena crear el AudioSource

            GameObject go = new GameObject($"SFX_{clip.name}");
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.volume = SfxVolume * volumeScale * attenuation;
            source.Play();

            Destroy(go, clip.length);
        }

#if UNITY_EDITOR
        // Compartido: lo llaman Door/ToggleButton/PressurePlate desde su propio
        // OnDrawGizmosSelected, para no repetir la búsqueda del AudioManager de la escena (que
        // fuera de Play no tiene Instance asignado) en cada uno de los tres scripts.
        public static void DrawProximityGizmo(Vector3 position)
        {
            AudioManager manager = Application.isPlaying ? Instance : FindFirstObjectByType<AudioManager>();
            if (manager == null) return;

            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(position, manager.sfxMinDistance);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(position, manager.sfxMaxDistance);
        }
#endif

        // Para EndingSequence: apaga la música ambient con el mismo fundido que la pantalla.
        public void StopMusic(float fadeTime)
        {
            if (musicSource == null) return;
            if (_musicRoutine != null) StopCoroutine(_musicRoutine);
            _musicRoutine = StartCoroutine(FadeMusicTo(null, fadeTime));
        }

        private AudioClip ResolveClipForRoom(string roomName)
        {
            AudioClip clip = defaultMusic;
            if (roomMusics != null)
            {
                foreach (var entry in roomMusics)
                {
                    if (entry != null && entry.roomName == roomName) { clip = entry.clip; break; }
                }
            }
            return clip;
        }

        private void HandleRoomChanged(string roomName)
        {
            AudioClip clip = ResolveClipForRoom(roomName);

            // Comparación por referencia: si la sala nueva resuelve al MISMO AudioClip que ya
            // está sonando (típicamente porque Default Music cubre ambas, o porque una entrada
            // de Room Musics repite el mismo clip), no se relanza — la pista sigue sonando sin
            // cortes de un extremo al otro de la partida, en vez de reiniciarse en cada sala.
            if (clip == musicSource.clip) return;

            if (_musicRoutine != null) StopCoroutine(_musicRoutine);
            _musicRoutine = StartCoroutine(FadeMusicTo(clip, musicCrossfadeTime));
        }

        private IEnumerator FadeMusicTo(AudioClip clip, float duration)
        {
            float half = duration * 0.5f;

            float t = 0f;
            float startVolume = musicSource.volume;
            while (t < half)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, half > 0f ? t / half : 1f);
                yield return null;
            }
            musicSource.volume = 0f;

            musicSource.clip = clip;
            if (clip != null) musicSource.Play();
            else musicSource.Stop();

            float target = MusicVolume;
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, target, half > 0f ? t / half : 1f);
                yield return null;
            }
            musicSource.volume = target;
            _musicRoutine = null;
        }
    }
}
