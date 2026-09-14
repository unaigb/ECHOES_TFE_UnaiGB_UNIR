using UnityEngine;

namespace Echoes.Saving
{
    // Banderas de traspaso entre la escena de Título y la de juego, y estado de sesión que no
    // se guarda en disco. No necesitan sobrevivir a un cierre de la aplicación — solo a un
    // SceneManager.LoadScene dentro de la misma sesión, que es justo lo que hace un campo
    // estático (no se resetea al cambiar de escena, solo al cerrar la aplicación).
    public static class GameFlow
    {
        public static bool ContinueRequested;

        // Se pone a true justo antes de cualquier SceneManager.LoadScene que abandona la partida
        // (Restart Room, detección, Main Menu). Mientras la escena se destruye, algún trigger
        // suelto (p. ej. una caja sobre una placa desapareciendo) puede disparar un sonido de
        // fracción de segundo que ya no tiene sentido — AudioManager lo consulta para no
        // reproducir nada mientras tanto. Se resetea a false en cuanto arranca la siguiente
        // escena (GameEntryPoint/TitleScreen), no hace falta tocarlo desde fuera de ahí.
        public static bool LeavingScene;

        // Aviso de autoguardado (pantalla de carga): se muestra una única vez por sesión, en el
        // primer cambio de sala real.
        public static bool AutosaveNoticeShown;

        // Cronómetro de partida: de cuando acaba la cinemática de entrada (o se salta, en
        // Continue/salto de depuración) a cuando acaba el diálogo de cierre. Se usa Time.time
        // (no Time.realtimeSinceStartup) a propósito: se congela solo con Time.timeScale = 0,
        // así que el tiempo en pausa no cuenta pero un reinicio por detección sí sigue sumando,
        // que es lo que se pide: el tiempo total hasta completar la demo, reintentos incluidos.
        private static float _timerStart = -1f;
        private static float _timerStop = -1f;

        public static void StartTimerIfNeeded()
        {
            if (_timerStart < 0f) _timerStart = Time.time;
        }

        public static void StopTimerIfNeeded()
        {
            if (_timerStop < 0f && _timerStart >= 0f) _timerStop = Time.time;
        }

        public static float ElapsedPlaytime =>
            _timerStart < 0f ? 0f : (_timerStop >= 0f ? _timerStop : Time.time) - _timerStart;

        // Llamado al arrancar una partida nueva desde el Título (New Game o Continue), para que
        // una partida anterior en la misma sesión de la app no deje tiempo o aviso arrastrado.
        public static void ResetSession()
        {
            AutosaveNoticeShown = false;
            _timerStart = -1f;
            _timerStop = -1f;
        }
    }
}
