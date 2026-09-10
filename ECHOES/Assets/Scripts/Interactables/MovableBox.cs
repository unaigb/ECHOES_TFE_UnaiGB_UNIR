using UnityEngine;

namespace Echoes.Interactables
{
    public class MovableBox : MonoBehaviour
    {
        [SerializeField] private bool isBig;
        public bool IsBig => isBig;
    }
}
