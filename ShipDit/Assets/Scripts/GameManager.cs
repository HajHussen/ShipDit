using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [System.Serializable]
    public class Player
    {
        public enum PlayerType
        {
            Human,
            AI
        }
        public PlayerType playerType;
        public Tile[,] myGrid = new Tile[10, 10];
        public bool[,] revealedGrid = new bool[10, 10];
        public Playfield playfield;

        [Space]
        public GameObject camPos;
        public GameObject placePanel;
        public GameObject shootPanel;
        [Space]
        public GameObject winPanel;
        public Player()
        {
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    OccupaationType type = OccupaationType.EMPTY;
                    myGrid[x, y] = new Tile(type, null);
                    revealedGrid[x, y] = false;
                }
            }
        }
        public List<GameObject> placedShipList = new List<GameObject>();
    }

    int activePlayer;
    public Player[] players = new Player[2];

    public enum GameState
    {
        P1_PLACE_SHIPS,
        P2_PLACE_SHIPS,
        SHOOTING,
        IDLE
    }
    public GameState gameState;

    public GameObject battleCamPos;

    bool camIsMoving;

    public GameObject placingCanvas;

    public bool isShooting;

    public GameObject rocketPrefab;
    float amplitude = 2f;
    float cTime;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        HideAllPanels();
        players[0].winPanel.SetActive(false);
        players[1].winPanel.SetActive(false);
        players[activePlayer].placePanel.SetActive(true);
        gameState = GameState.IDLE;


    }

    void AddShipToList(GameObject placedShip)
    {
        players[activePlayer].placedShipList.Add(placedShip);
    }

    public void UpdateGrid(Transform shipTransform, ShipBehavior ship, GameObject placedShip)
    {
        foreach (Transform child in shipTransform)
        {
            TileInfo tInfo = child.GetComponent<GhostBehavior>().GetTileInfo();
            players[activePlayer].myGrid[tInfo.xPos, tInfo.zPos] = new Tile(ship.type, ship);
        }
        AddShipToList(placedShip);
    }
    public bool CheckIfOccupied(int xPos, int zPos)
    {
        return players[activePlayer].myGrid[xPos, zPos].IsOccupied();
    }

    public void DeleteAllShips()
    {
        foreach (var item in players[activePlayer].placedShipList)
        {
            Destroy(item);
        }
        players[activePlayer].placedShipList.Clear();
        InitGrid();
    }

    void InitGrid()
    {
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                OccupaationType type = OccupaationType.EMPTY;
                players[activePlayer].myGrid[x, y] = new Tile(type, null);
            }
        }
    }

    private void Update()
    {
        switch (gameState)
        {
            case GameState.P1_PLACE_SHIPS:
                players[activePlayer].placePanel.SetActive(false);

                PlacingManager.Instance.SetPlayer(players[activePlayer].playfield, players[activePlayer].playerType.ToString());
                StartCoroutine(MoveCamera(players[activePlayer].camPos));
                gameState = GameState.IDLE;
                break;
            case GameState.P2_PLACE_SHIPS:
                players[activePlayer].placePanel.SetActive(false);

                PlacingManager.Instance.SetPlayer(players[activePlayer].playfield, players[activePlayer].playerType.ToString());
                gameState = GameState.IDLE;
                break;
            case GameState.SHOOTING:
                if (players[activePlayer].playerType == Player.PlayerType.AI)
                {

                }
                break;
            case GameState.IDLE:
                break;
            default:
                break;
        }
    }

    void HideAllPanels()
    {
        players[0].placePanel.SetActive(false);
        players[0].shootPanel.SetActive(false);

        players[1].placePanel.SetActive(false);
        players[1].shootPanel.SetActive(false);
    }

    public void P1PlaceShips()
    {
        gameState = GameState.P1_PLACE_SHIPS;
    }

    public void P2PlaceShips()
    {
        gameState = GameState.P2_PLACE_SHIPS;
    }

    public void PlacingReady()
    {
        if (activePlayer == 0)
        {
            HideAllMyShips();
            SwitchPlayer();
            if (players[activePlayer].playerType==Player.PlayerType.AI)
            {
                gameState = GameState.P2_PLACE_SHIPS;
                StartCoroutine(MoveCamera(battleCamPos));
                return;
            }
            StartCoroutine(MoveCamera(players[activePlayer].camPos));
            players[activePlayer].placePanel.SetActive(true);
            return;
        }
        if (activePlayer == 1)
        {
            HideAllMyShips();
            SwitchPlayer();
            StartCoroutine(MoveCamera(battleCamPos));
            players[activePlayer].shootPanel.SetActive(true);
            placingCanvas.SetActive(false);
        }
    }

    void HideAllMyShips()
    {
        foreach (var ship in players[activePlayer].placedShipList)
        {
            ship.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    void UnHideAllMyShips()
    {
        foreach (var ship in players[activePlayer].placedShipList)
        {
            ship.GetComponent<MeshRenderer>().enabled = true;
        }
    }

    void SwitchPlayer()
    {
        activePlayer++;
        activePlayer %= 2;
    }

    IEnumerator MoveCamera(GameObject camObj)
    {
        if (camIsMoving)
        {
            yield break;
        }
        camIsMoving = true;

        float t = 0;
        float duration = 0.5f;

        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;

        Vector3 endPos = camObj.transform.position;
        Quaternion endPosRot = camObj.transform.rotation;

        while (t < duration)
        {
            t += Time.deltaTime;
            Camera.main.transform.position = Vector3.Lerp(startPos, endPos, t / duration);
            Camera.main.transform.rotation = Quaternion.Lerp(startRot, endPosRot, t / duration);
            yield return null;
        }
        camIsMoving = false;
    }

    public void ShotButton()
    {
        UnHideAllMyShips();
        players[activePlayer].shootPanel.SetActive(false);
        gameState = GameState.SHOOTING;
    }

    int Opponent()
    {
        int me = activePlayer;
        me++;
        me %= 2;
        int opponent = me;
        return opponent;
    }

    public void CheckShot(int x, int z, TileInfo info)
    {
        StartCoroutine(CheckCoordinate(x, z, info));
    }

    IEnumerator CheckCoordinate(int x, int z, TileInfo info)
    {
        if (isShooting)
        {
            yield break;
        }
        isShooting = true;

        int opponent = Opponent();

        if (!players[opponent].playfield.RequestTile(info))
        {
            isShooting = false;
            yield break;
        }

        if (players[opponent].revealedGrid[x, z])
        {
            isShooting = false;
            yield break;
        }

        Vector3 startPos = Vector3.zero;
        Vector3 goalPos = info.gameObject.transform.position;

        GameObject rocket = Instantiate(rocketPrefab, startPos, Quaternion.identity);

        while (MoveInArcToTile(startPos, goalPos, .8f, rocket))
        {
            yield return null;
        }

        Destroy(rocket);
        cTime = 0;

        if (players[opponent].myGrid[x, z].IsOccupied())
        {
            bool sunk = players[opponent].myGrid[x, z].placedShip.TakeDamage();
            if (sunk)
            {
                players[opponent].placedShipList.Remove(players[opponent].myGrid[x, z].placedShip.gameObject);
            }

            info.ActivateHighlight(3, true);
        }
        else
        {
            info.ActivateHighlight(2, true);
        }
        players[opponent].revealedGrid[x, z] = true;

        if (players[opponent].placedShipList.Count == 0)
        {
            print("You WIN!");
            players[activePlayer].winPanel.SetActive(true);
            yield break;
        }

        yield return new WaitForSeconds(1.2f);
        HideAllMyShips();
        SwitchPlayer();

        if (players[activePlayer].playerType == Player.PlayerType.AI)
        {
            isShooting = false;
            gameState = GameState.IDLE;
            AIShot();
            yield break;
        }

        players[activePlayer].shootPanel.SetActive(true);
        gameState = GameState.IDLE;
        isShooting = false;
    }

    bool MoveInArcToTile(Vector3 startPos, Vector3 goalPos, float speed, GameObject rocket)
    {
        cTime += speed * Time.deltaTime;
        Vector3 myPos = Vector3.Lerp(startPos, goalPos, cTime);
        myPos.y = amplitude * Mathf.Sin(Mathf.Clamp01(cTime) * Mathf.PI);
        rocket.transform.LookAt(myPos);

        return goalPos != (rocket.transform.position = Vector3.Lerp(rocket.transform.position, myPos, cTime));
    }

    void AIShot()
    {
        int index = 0;
        int x = 0;
        int z = 0;

        TileInfo info = null;

        int opponent = Opponent();

        List<int[]> partiallyRevealedTiles = new List<int[]>();

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (players[opponent].revealedGrid[i, j])
                {
                    if (players[opponent].myGrid[i, j].IsOccupied())
                    {
                        if (players[opponent].myGrid[i, j].placedShip.IsHit())
                        {
                            partiallyRevealedTiles.Add(new int[2] { i, j });
                        }
                    }
                }
            }
        }

        List<int[]> neighborList = new List<int[]>();
        if (partiallyRevealedTiles.Count > 0)
        {
            for (int i = 0; i < partiallyRevealedTiles.Count; i++)
            {
                neighborList.AddRange(GetNeighbors(partiallyRevealedTiles[i]));
            }
            index = Random.Range(0, neighborList.Count);
            x = neighborList[index][0];
            z = neighborList[index][1];

            info = players[opponent].playfield.GetTileInfo(x, z);

            CheckShot(x, z, info);
            return;
        }

        List<int[]> randomTileList = new List<int[]>();
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (!players[opponent].revealedGrid[i, j])
                {
                    randomTileList.Add(new int[2] { i, j });
                }
            }
        }

        index = Random.Range(0, randomTileList.Count);
        x = randomTileList[index][0];
        z = randomTileList[index][1];

        info = players[opponent].playfield.GetTileInfo(x, z);

        CheckShot(x, z, info);
    }

    List<int[]> GetNeighbors(int[] originalCoords)
    {
        List<int[]> neighbors = new List<int[]>();
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue;
                if (x == 1 && z == 1) continue;
                if (x == -1 && z == 1) continue;
                if (x == -1 && z == -1) continue;
                if (x == 1 && z == -1) continue;

                int checkX = originalCoords[0] + x;
                int checkZ = originalCoords[1] + z;

                if (checkX >= 0 && checkX < 10 && checkZ >= 0 && checkZ < 10 && !players[Opponent()].revealedGrid[checkX, checkZ])
                {
                    neighbors.Add(new int[2] { checkX, checkZ });
                }
            }
        }
        return neighbors;
    }
}
