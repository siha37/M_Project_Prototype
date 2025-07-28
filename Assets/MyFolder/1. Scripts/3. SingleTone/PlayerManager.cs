using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class PlayerManager : SingleTone<PlayerManager>
    {
        List<GameObject> players = new List<GameObject>();
        private int currentPlayer = 0;
        private void Start()
        {
            players = GameObject.FindGameObjectsWithTag("Player").ToList();
        }

        public GameObject GetPlayer()
        {
            if (players.Count <= 0)
                return null; 
            GameObject result = players[currentPlayer];
            currentPlayer++;
            currentPlayer = currentPlayer % players.Count;
            return result;
        }
    }
}
