using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace MatchThreeEngine
{
    public sealed class Board : MonoBehaviour
    {
		public int currentLevel = 1;
        public Button[] levelButtons;
        public int unlockedLevels = 1;
        public GameObject levelMenu;
        [SerializeField] private TileTypeAsset[] tileTypes;
        [SerializeField] private Row[] rows;
        [SerializeField] private AudioClip matchSound;
        [SerializeField] private AudioSource matchAudioSource;
        [SerializeField] private float tileTweenDuration;
        [SerializeField] private Transform swapOverlay;
        [SerializeField] private bool ensureNoStartingMatches;
        [SerializeField] private Slider gameTimerSlider;
        [SerializeField] private float maxGameDuration = 120f;
        [SerializeField] private Slider winTimerSlider;
        [SerializeField] private Slider loseGoalSlider;
        [SerializeField] private Slider scoreProgressSlider;
		private readonly List<Tile> selectedTiles = new List<Tile>();
        private bool isSwapping;
        public GameObject eventGameObject;
        private bool isMatching;
        private bool isShuffling;
        private bool isGameRunning;
        public int currentScore;
        public GameObject winPanel;
        public GameObject losePanel;
        private float remainingTime;
        private Coroutine timerCoroutine;
        

        public event Action<TileTypeAsset, int> OnMatch;

        private TileData[,] Matrix
        {
            get
            {
                var width = rows.Max(row => row.tiles.Length);
                var height = rows.Length;

                var data = new TileData[width, height];

                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                        data[x, y] = GetTile(x, y).Data;

                return data;
            }
        }

        // init. the game
        public void StartGame()
        {
            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 0; x < rows.Max(row => row.tiles.Length); x++)
                {
                    var tile = GetTile(x, y);

                    tile.x = x;
                    tile.y = y;

                    tile.Type = tileTypes[Random.Range(0, tileTypes.Length)];

                    tile.button.onClick.AddListener(() => Select(tile));
                }
            }
            currentScore = 0;
            if (ensureNoStartingMatches) StartCoroutine(EnsureNoStartingMatches());
            StopGameTimer();

            remainingTime = maxGameDuration;
            gameTimerSlider.maxValue = maxGameDuration;
            gameTimerSlider.value = remainingTime;
            winTimerSlider.maxValue = maxGameDuration;
            loseGoalSlider.maxValue = 100;
            scoreProgressSlider.maxValue = 100;
            scoreProgressSlider.value = 20; // start value to avoid slider's bug

            StartGameTimer(); // activate slider + timer
        }

        // Method to stop the game timer
        public void StopGameTimer()
        {
            isGameRunning = false;
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
            }
        }

        // Method to start the game timer
        public void StartGameTimer()
        {
            isGameRunning = true;
            timerCoroutine = StartCoroutine(GameTimerCoroutine());
        }

        // handle the game timer countdown
        private IEnumerator GameTimerCoroutine()
        {
            while (remainingTime > 0 && isGameRunning)
            {
                remainingTime -= Time.deltaTime;
                gameTimerSlider.value = remainingTime;
                if (remainingTime <= 0)
                {
                    HandleGameLost();
                }

                yield return null;
            }
        }

        // win
        private void HandleGameWon()
        {
            winTimerSlider.value = remainingTime;
            winPanel.SetActive(true);
            StopGameTimer();
        }

        // lose
        private void HandleGameLost()
        {
            losePanel.SetActive(true);
            StopGameTimer();
            loseGoalSlider.value = scoreProgressSlider.value; // Set loseGoalSlider to the current scoreProgressSlider value
        }

        // ++level
        public void AdvanceToNextLevel()
        {
            if (currentLevel < 12)
            {
                currentLevel += 1;
				unlockedLevels++;
				SaveLevels();
            	UpdateButtonInteractivity();
            }
            StartGame();
        }

        // reshuffle
        public void ResetBoard()
        {
            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 0; x < rows.Max(row => row.tiles.Length); x++)
                {
                    var tile = GetTile(x, y);

                    tile.x = x;
                    tile.y = y;

                    tile.Type = tileTypes[Random.Range(0, tileTypes.Length)];

                    tile.button.onClick.AddListener(() => Select(tile));
                }
            }
            if (ensureNoStartingMatches) StartCoroutine(EnsureNoStartingMatches()); // for button of reset
        }

        // levelsave
        private void SaveLevels()
        {
            PlayerPrefs.SetInt("LevelsCompleted", unlockedLevels);
            PlayerPrefs.Save();
        }

        // load
        private void LoadLevels()
        {
            unlockedLevels = PlayerPrefs.GetInt("LevelsCompleted", 1);
        }

        // Method to update the interactivity of level selection buttons
        private void UpdateButtonInteractivity()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                levelButtons[i].interactable = i < unlockedLevels;
            }
        }

        private void Start()
        {
            LoadLevels();
            UpdateButtonInteractivity();
        }

        // to show current level
        public void SelectLevel(int levelSelected)
        {
            currentLevel = levelSelected;
            levelMenu.SetActive(false);
            StartGame();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        // Method to handle updates in the game, specifically for checking user input
        private void Update()
        {
			//not used yet
        }

        // Coroutine to ensure no starting matches on the board
        private IEnumerator EnsureNoStartingMatches()
        {
            var wait = new WaitForEndOfFrame();

            while (TileDataMatrixUtility.FindBestMatch(Matrix) != null)
            {
                ShuffleBoard();

                yield return wait;
            }
        }

        // Method to get a specific tile at given coordinates
        private Tile GetTile(int x, int y) => rows[y].tiles[x];

        // Method to get tiles from a list of tile data
        private Tile[] GetTiles(IList<TileData> tileData)
        {
            var length = tileData.Count;

            var tiles = new Tile[length];

            for (var i = 0; i < length; i++) tiles[i] = GetTile(tileData[i].X, tileData[i].Y);

            return tiles;
        }

        // Method to handle tile selection and swapping logic
        private async void Select(Tile tile)
        {
            if (isSwapping || isMatching || isShuffling)
            {
                Debug.Log("Action in progress, selection ignored.");
                return;
            }

            if (!selectedTiles.Contains(tile))
            {
                if (selectedTiles.Count > 0)
                {
                    if (Math.Abs(tile.x - selectedTiles[0].x) == 1 && Math.Abs(tile.y - selectedTiles[0].y) == 0
                        || Math.Abs(tile.y - selectedTiles[0].y) == 1 && Math.Abs(tile.x - selectedTiles[0].x) == 0)
                    {
                        selectedTiles.Add(tile);
                    }
                }
                else
                {
                    selectedTiles.Add(tile);
                }
            }

            if (selectedTiles.Count < 2) return;

            isSwapping = true;
            bool success = await SwapAndMatchAsync(selectedTiles[0], selectedTiles[1]);
            if (!success)
            {
                await SwapAsynchron(selectedTiles[0], selectedTiles[1]);
            }
            isSwapping = false;

            selectedTiles.Clear();
            EnsurePlayableBoard();
        }

        // Method to swap and match tiles asynchronously
        private async Task<bool> SwapAndMatchAsync(Tile tile1, Tile tile2)
        {
            await SwapAsynchron(tile1, tile2);

            if (await TryMatchAsync())
            {
                return true;
            }

            return false;
        }

        // Method to swap tiles asynch
        private async Task SwapAsynchron(Tile tile1, Tile tile2)
        {
            var icon1 = tile1.icon;
            var icon2 = tile2.icon;
            var icon1Transform = icon1.transform;
            var icon2Transform = icon2.transform;
            icon1Transform.SetParent(swapOverlay);
            icon2Transform.SetParent(swapOverlay);
            icon1Transform.SetAsLastSibling();
            icon2Transform.SetAsLastSibling();
            icon1Transform.SetParent(tile2.transform);
            icon2Transform.SetParent(tile1.transform);
            tile1.icon = icon2;
            tile2.icon = icon1;

            var tile1Item = tile1.Type;
            tile1.Type = tile2.Type;
            tile2.Type = tile1Item;
        }

        // Method to ensure the board has playable moves
        private void EnsurePlayableBoard()
        {
            var matrix = Matrix;

            while ( TileDataMatrixUtility.FindBestMatch(matrix) != null)
            {
                ShuffleBoard();
                matrix = Matrix;
            }
        }

        // Method to shuffle the tiles on the board
        private void ShuffleBoard()
        {
            isShuffling = true;

            foreach (var row in rows)
                foreach (var tile in row.tiles)
                    tile.Type = tileTypes[Random.Range(0, tileTypes.Length)];

            isShuffling = false;
        }

        // Method to update the score
        private void IncreaseScore()
        {
            currentScore += 50;
            scoreProgressSlider.value += 4; // Increase the score slider value by 4%
            if (scoreProgressSlider.value >= 100)
            {
                HandleGameWon();
            }
        }

        // Method to try matching tiles asynchronously
        private async Task<bool> TryMatchAsync()
        {
            var didMatch = false;

            isMatching = true;

            var match = TileDataMatrixUtility.FindBestMatch(Matrix);

            while (match != null)
            {
                didMatch = true;

                var tiles = GetTiles(match.Tiles);

                var deflateSequence = DOTween.Sequence();

                foreach (var tile in tiles) deflateSequence.Join(tile.icon.transform.DOScale(Vector3.zero, tileTweenDuration).SetEase(Ease.InBack));

                matchAudioSource.PlayOneShot(matchSound);
                IncreaseScore();

                await deflateSequence.Play().AsyncWaitForCompletion();

                var inflateSequence = DOTween.Sequence();

                foreach (var tile in tiles)
                {
                    tile.Type = tileTypes[Random.Range(0, tileTypes.Length)];

                    inflateSequence.Join(tile.icon.transform.DOScale(Vector3.one, tileTweenDuration).SetEase(Ease.OutBack));
                }

                await inflateSequence.Play().AsyncWaitForCompletion();

                OnMatch?.Invoke(Array.Find(tileTypes, tileType => tileType.id == match.TypeId), match.Tiles.Length);

                match = TileDataMatrixUtility.FindBestMatch(Matrix);
            }
            isMatching = false;

            return didMatch;
        }

        // Method to handle button activation
        public void ButtonActivated()
        {
            eventGameObject.SetActive(false);
            StartCoroutine("ResetEvent", 0.1f);
        }
		public void AfterGameWin()
        {
            if (unlockedLevels < 12)
            {
                unlockedLevels++;
            }
            SaveLevels();
            UpdateButtonInteractivity();
        }

        // Coroutine to reset the event game object
        public IEnumerator ResetEvent()
        {
            eventGameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
