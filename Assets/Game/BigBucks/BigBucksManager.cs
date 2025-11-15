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
            playersLeft = PlayerManager.GetNumPlayers();
        }
        

        public void PlayerFalls(int playerIndex) {
            Debug.Log(playerIndex + " fell and got " + playersLeft + "st/nd/rd/th place");
            ranking[playerIndex] = playersLeft;
            playersLeft--;
            if (playersLeft <= 1) {
                MinigameManager.instance.EndMinigame(ranking);
            }
        }
    }
}
