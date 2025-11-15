using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using UnityEngine;

namespace CountyFair.BigBucks {
    public class BigBucksManager : MonoBehaviour {

        public static BigBucksManager inst;
        void Awake() {
            inst = this;
        }

        private MinigameManager.Ranking ranking = new();
        private int playersLeft;

        void Start() {
            // record num players when first one falls
            playersLeft = -1;
        }
        

        public void PlayerFalls(int playerIndex) {
            if (playersLeft == -1) playersLeft = PlayerManager.GetNumPlayers();
            Debug.Log(playerIndex + " fell and got " + playersLeft + "st/nd/rd/th place");
            ranking[playerIndex] = playersLeft;
            playersLeft--;
            if (playersLeft <= 1) {
                for (int i = 0; i < PlayerManager.GetNumPlayers(); i++) {
                    if (ranking[i] == 0) ranking[i] = 1; // set alive to first place
                }
                MinigameManager.instance.EndMinigame(ranking);
            }
        }
    }
}
