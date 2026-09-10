using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private PlayerController player;
    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

 
}
