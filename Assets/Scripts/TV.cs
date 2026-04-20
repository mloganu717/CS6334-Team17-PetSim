// using UnityEngine;
// using UnityEngine.Video;

// // turns off immediately when player looks at it
// public class TV : MonoBehaviour
// {
//     [SerializeField] private VideoPlayer videoPlayer;
//     [SerializeField] private Transform playerCamera; // assign main camera
//     [SerializeField] private float detectionAngle = 30f; // how wide the looking 
//     private bool playerIsLooking;

//     private void Start()
//     {
//         if (playerCamera == null && Camera.main != null)
//             playerCamera = Camera.main.transform;

//         // start with tv off
//         if (videoPlayer != null) videoPlayer.Stop();
//     }

//     private void Update()
//     {
//         if (playerCamera == null) return;

//         // check if player is looking at the tv
//         Vector3 directionToTV = transform.position - playerCamera.position;
//         float angle = Vector3.Angle(playerCamera.forward, directionToTV);

//         bool lookingNow = angle < detectionAngle;

//         if (lookingNow && !playerIsLooking)
//         {
//             // player just looked at it - turn off immediately
//             playerIsLooking = true;
//             TurnOff();
//         }
//         else if (!lookingNow && playerIsLooking)
//         {
//             // player looked away - turn on
//             playerIsLooking = false;
//             TurnOn();
//         }
//     }

//     private void TurnOn()
//     {
//         if (videoPlayer != null) videoPlayer.Play();
//     }

//     private void TurnOff()
//     {
//         if (videoPlayer != null) videoPlayer.Pause();
//     }
// }