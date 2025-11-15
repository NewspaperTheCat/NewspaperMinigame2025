using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace CountyFair.BigBucks {
    public class SequenceController : MonoBehaviour {

        // Player Binding
        public int playerIndex;
        public UnityEvent playerMissed;

        // Indicator display
        [Header("Sequence Display")]
        public float indicatorDuration;
        float indicatorSpeed; // set in start
        [SerializeField] GameObject indicatorPrefab;

        // index mappings
        private List<String> indicatorIndices = new() { "ButtonA", "ButtonB", "ButtonL", "ButtonR" };
        [SerializeField] List<Sprite> indicatorSprites = new();
        [SerializeField] Transform start, end;
        private List<Transform> indicators = new();

        // Sequence Interaction
        [Header("Sequence Interaction")]
        public float spawnInterval;
        [SerializeField] float inputWindow;
        
        void Start() {
            indicatorSpeed = Vector2.Distance(end.position, start.position) / indicatorDuration;
            Invoke("SpawnIndicatorLoop", spawnInterval);
        }
        
        void SpawnIndicatorLoop() {
            SpawnIndicator();

            spawnInterval *= .985f;
            indicatorDuration *= .985f;
            Invoke("SpawnIndicatorLoop", spawnInterval);
            if (UnityEngine.Random.Range(0, 10) > 5)
                Invoke("SpawnIndicator", spawnInterval * 1.375f);
        }
        
        void SpawnIndicator() {
            String chosenButton = indicatorIndices[UnityEngine.Random.Range(0, indicatorIndices.Count)];
            CreateIndicator(chosenButton);
        }

        void Update() {
            float speed = indicatorSpeed * Time.deltaTime;
            for (int i = 0; i < indicators.Count; i++) {
                Transform ind = indicators[i];
                Vector2 dir = Vector2.down;
                ind.position += (Vector3)(dir * speed);

                // Check for if missed opportunity
                if (end.position.y > ind.position.y) {
                    playerMissed.Invoke();
                    indicators.RemoveAt(i);
                    Destroy(ind.gameObject);
                    i--;
                } else if (ind.position.y - inputWindow < end.position.y) {
                    ind.GetComponent<SpriteRenderer>().color = Color.green;
                }
            }
        }

        private void CreateIndicator(String button) {
            int index = indicatorIndices.IndexOf(button);
            Transform i = Instantiate(indicatorPrefab, transform).transform;
            i.position = (Vector2)start.position; // z value of position contains player index
            i.position += Vector3.forward * playerIndex;
            i.position += Vector3.right * (index - 1.5f) * 4.5f / 5f; 
            i.GetComponent<SpriteRenderer>().sprite = indicatorSprites[index];
            i.name = button;

            indicators.Add(i);
        }

        // -1 = error, 1 = success, 0 = ignore
        public int EvaluateInput(String input) {
            if (indicators.Count == 0 || indicators[0].name != input) return 0;

            float distance = Mathf.Abs(end.position.y - indicators[0].position.y);
            float remainingDuration = distance / indicatorSpeed;

            int validity = 0;
            if (distance > inputWindow * 2) {
                validity = 0;
            } else if (distance > inputWindow) {
                Debug.Log("Too Early");
                PopIndicator();
                validity = -1;
            } else {
                Debug.Log("Correct!");
                PopIndicator();
                validity = 1;
            }
            return validity;
        }
        
        private void PopIndicator() {
            Destroy(indicators[0].gameObject);
            indicators.RemoveAt(0);
        }
    }
}
