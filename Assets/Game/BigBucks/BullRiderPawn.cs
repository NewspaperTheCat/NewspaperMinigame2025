using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Game.Examples;
using Unity.VisualScripting;

namespace CountyFair.BigBucks {
    public class BullRiderPawn : Pawn {

        // Bull Bucking Variables
        [Header("Bull Parameters")]
        [SerializeField] SpriteRenderer bullRenderer;
        [SerializeField] List<Sprite> bullSprites = new List<Sprite>();
        float buck = 0; // value swining between 0 and 1, speed determined by input spawning speed
        float bullAngle = 0;
        float angularVel = 0;
        float buckTimer = 0;
        float buckTimeAlignment;

        // Player Stability Variables
        [Header("Player Parameters")]
        [SerializeField] SpriteRenderer playerRenderer;
        [SerializeField] List<Sprite> playerSprites = new List<Sprite>();
        private int _instability = 0;
        int instability {
            get { return _instability; }
            set { _instability = Math.Clamp(value, 0, fallThreshold); }
        }
        [SerializeField] int staggerThreshold = 5;
        [SerializeField] int fallThreshold = 10;
        [SerializeField] bool fallen = false;

        // Project Communication
        [SerializeField] SequenceController sc;

        // Effects
        [SerializeField] ParticleSystem sweatBurst, passiveSweat;


        void Start() {
            buckTimeAlignment = Mathf.PI - sc.indicatorDuration - sc.spawnInterval;
            ChangeBullDir();
        }


        void Update() {
            buckTimer += Time.deltaTime / sc.indicatorDuration / sc.spawnInterval;
            buck = Mathf.Cos(buckTimer * 2 * Mathf.PI + buckTimeAlignment);

            bullAngle = (360 + bullAngle + angularVel * Time.deltaTime) % 360;
            angularVel -= angularVel * Time.deltaTime;
        }

        void LateUpdate() {
            UpdateSprites();
        }
        
        private void UpdateSprites() {
            // Update bullRenderer sprite based on buck and rotation
            int bullSpriteFrame = 0;

            // Map Angles to Frames
            float angleOffset = bullAngle + 22.5f;
            if (angleOffset < 45) { // EAST
                bullSpriteFrame = 0;
                bullRenderer.flipX = false;
            } else if (angleOffset < 90) { // NORTH EAST
                bullSpriteFrame = 3;
                bullRenderer.flipX = false;
            } else if (angleOffset < 135) { // NORTH
                bullSpriteFrame = 6;
                bullRenderer.flipX = false;
            } else if (angleOffset < 180) { // NORTH WEST
                bullSpriteFrame = 3;
                bullRenderer.flipX = true;
            } else if (angleOffset < 225) { // WEST
                bullSpriteFrame = 0;
                bullRenderer.flipX = true;
            } else if (angleOffset < 270) { // SOUTH WEST
                bullSpriteFrame = 9;
                bullRenderer.flipX = false;
            } else if (angleOffset < 315) { // SOUTH
                bullSpriteFrame = 12;
                bullRenderer.flipX = false;
            } else if (angleOffset < 360) { // SOUTH EAST
                bullSpriteFrame = 9;
                bullRenderer.flipX = true;
            } else { // EAST (from other side)
                bullSpriteFrame = 0;
                bullRenderer.flipX = false;
            }

            int bullSpriteBuck = 0;
            if (buck > .333f) {
                bullSpriteBuck = 2;
            } else if (buck >= -.333f) {
                bullSpriteBuck = 1;   
            }

            bullRenderer.sprite = bullSprites[bullSpriteFrame + bullSpriteBuck];

            if (!fallen) {
                // Update Player
                float quantizedBuck = Mathf.Round(buck * 3f) / 3f;
                playerRenderer.transform.position += Vector3.up * (quantizedBuck * .5f + .75f) - Vector3.up * playerRenderer.transform.position.y;

                // Update ordering
                bool playerOnTop = false;
                if (bullSpriteFrame == 0) {
                    playerOnTop = true;
                } else if (angleOffset >= 225 && angleOffset < 360) { // facing at least somewhat south
                    playerOnTop = bullSpriteBuck >= 1;
                } else {
                    playerOnTop = bullSpriteBuck < 1;
                }
                
                if (!playerOnTop) {
                    bullRenderer.sortingOrder = 3;
                    playerRenderer.sortingOrder = 2;
                } else {
                    bullRenderer.sortingOrder = 2;
                    playerRenderer.sortingOrder = 3;
                }
            } else if (playerRenderer.transform.position.y > -10) {
                playerRenderer.transform.position += (Vector3)playerFallVel * Time.deltaTime;
                playerFallVel += Vector2.down * fallGravity * Time.deltaTime;
            }
        }
        

        protected override void OnActionPressed(InputAction.CallbackContext context) {
            if (fallen) return;
            String input = context.action.name;

            if (input == PawnAction.ButtonX) input = PawnAction.ButtonB;
            else if (input == PawnAction.ButtonY) input = PawnAction.ButtonA;
            else if (!(input == PawnAction.ButtonA
                || input == PawnAction.ButtonB
                || input == PawnAction.ButtonL
                || input == PawnAction.ButtonR)) return;


            Debug.Log(input);


            int outcome = sc.EvaluateInput(input);
            if (outcome > 0) {
                InputSucceeded();
            } else if (outcome < 0) {
                InputFailed();
            } // ignore on outcome = 0
        }
        
        public void InputFailed() {
            if (fallen) return;

            BBAudio.inst.PlayRandomWrong();
            sweatBurst.Play();

            instability += 2;
            if (instability >= fallThreshold) {
                Fall();
            } else if (instability > staggerThreshold) {
                playerRenderer.color = Color.red;

                ParticleSystem.EmissionModule em = passiveSweat.emission;
                em.rateOverTimeMultiplier = 4;
                em.rateOverDistanceMultiplier = 4;
            }
        }

        private void InputSucceeded() {
            instability -= 1; // rewards for success, regain control
            BBAudio.inst.PlayRandomCorrect();

            if (instability <= staggerThreshold) {
                playerRenderer.color = Color.white;

                ParticleSystem.EmissionModule em = passiveSweat.emission;
                em.rateOverTimeMultiplier = 1;
                em.rateOverDistanceMultiplier = 1;
            }
        }
        
        private Vector2 playerFallVel;
        private float fallGravity = 10f;
        private void Fall() {
            fallen = true;

            float launchAngle = UnityEngine.Random.Range(0, Mathf.PI * .4f) + Mathf.PI * .3f;
            playerFallVel = new Vector2(Mathf.Cos(launchAngle), Mathf.Sin(launchAngle)) * 10;
            bullRenderer.sortingOrder = 2;
            playerRenderer.sortingOrder = 3;

            BBAudio.inst.PlayRandomBucked();
            BigBucksManager.inst.PlayerFalls(playerIndex);
        }
        
        private void ChangeBullDir() {
            // magnitude * random direction
            angularVel = UnityEngine.Random.Range(480f, 720f) * (UnityEngine.Random.Range(0, 2) * 2 - 1);
            Invoke("ChangeBullDir", UnityEngine.Random.Range(.5f, 2f));
        }
    }
}