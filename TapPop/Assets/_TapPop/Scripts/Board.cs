using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TapPop
{
    // Tap-to-pop board: tap 2+ touching tiles of one colour, they pop, the rest fall and new tiles drop in.
    // Where tiles land, big enough groups pop by themselves (chains) until the board is still.
    public class Board : MonoBehaviour
    {
        [SerializeField] TileSet tileSet;
        [SerializeField] TileView tilePrefab;
        [SerializeField] RectTransform tileRoot;
        [SerializeField] PopFx fx;
        [SerializeField] Sfx sfx;
        [SerializeField] float cellSize = 120f;
        [Tooltip("Drawn size of a tile; the difference to cellSize is the gap")]
        [SerializeField] float tileSize = 112f;

        // Colour index, the popped tiles and the chain step (0 = the tap itself), raised before they are removed.
        public event Action<int, IReadOnlyList<TileView>, int> GroupPopped;
        // Raised when a move has fully played out and the board is still.
        public event Action Settled;

        public bool InputEnabled { get; set; }
        public bool AcceptsInput => InputEnabled && !busy;

        static readonly (int dx, int dy)[] Neighbours = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        readonly Stack<TileView> pool = new Stack<TileView>();
        LevelConfig level;
        TileView[,] grid;
        bool busy;

        int Width => level.width;
        int Height => level.height;

        public void Build(LevelConfig config)
        {
            level = config;
            if (level == null || tileSet == null || Width < 2 || Height < 2 || level.colorCount < 1)
            {
                Debug.LogError("TapPop: Board is missing its Level Config or Tile Set.", this);
                return;
            }

            StopAllCoroutines();
            busy = true;
            if (grid != null)
                foreach (TileView tile in grid)
                    if (tile != null) Release(tile);

            grid = new TileView[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    Spawn(x, y, RandomColor());
            while (!HasMove())
                foreach (TileView tile in grid) tile.Setup(this, RandomColor(), tileSet);

            float longest = 0f;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    TileView tile = grid[x, y];
                    tile.Rect.anchoredPosition = CellPosition(x, y + Height + 0.5f);
                    float delay = y * 0.04f + x * 0.015f;
                    float duration = FallTime(Height + 0.5f);
                    tile.MoveTo(CellPosition(x, y), duration, delay);
                    longest = Mathf.Max(longest, delay + duration);
                }
            }
            StartCoroutine(Unlock(longest + 0.15f));
        }

        public void OnTileTapped(TileView tile)
        {
            if (!AcceptsInput) return;
            List<TileView> group = FindGroup(tile);
            if (group.Count < 2)
            {
                tile.Shake();
                return;
            }
            StartCoroutine(Resolve(tile, group));
        }

        IEnumerator Resolve(TileView origin, List<TileView> group)
        {
            busy = true;
            GroupPopped?.Invoke(origin.ColorIndex, group, 0);
            yield return new WaitForSeconds(Pop(origin, group, 0));
            Remove(group);

            var landed = new List<TileView>();
            yield return new WaitForSeconds(Collapse(landed) + 0.1f);

            for (int step = 1; step <= level.maxChainSteps && level.chainGroupSize >= 2; step++)
            {
                List<List<TileView>> chains = FindChains(landed);
                if (chains.Count == 0) break;

                float popEnd = 0f;
                foreach (List<TileView> chain in chains)
                {
                    GroupPopped?.Invoke(chain[0].ColorIndex, chain, step);
                    popEnd = Mathf.Max(popEnd, Pop(chain[0], chain, step));
                }
                yield return new WaitForSeconds(popEnd);
                foreach (List<TileView> chain in chains) Remove(chain);

                landed.Clear();
                yield return new WaitForSeconds(Collapse(landed) + 0.1f);
            }

            if (!HasMove()) yield return Shuffle();

            busy = false;
            Settled?.Invoke();
        }

        // Pops outwards from the origin tile, like a ripple, one sound per ring. Returns when the last tile is gone.
        float Pop(TileView origin, List<TileView> group, int chainStep)
        {
            float popEnd = 0f;
            var rings = new HashSet<int>();
            foreach (TileView tile in group)
            {
                int ring = Mathf.Abs(tile.X - origin.X) + Mathf.Abs(tile.Y - origin.Y);
                float delay = ring * 0.035f;
                tile.Pop(delay);
                fx.Burst(tile.transform.position, tileSet.FxColor(tile.ColorIndex), delay + 0.07f);
                if (rings.Add(ring)) sfx.Pop(delay + 0.05f, ring + chainStep * 2);
                popEnd = Mathf.Max(popEnd, delay + 0.19f);
            }
            return popEnd;
        }

        void Remove(List<TileView> group)
        {
            foreach (TileView tile in group)
            {
                grid[tile.X, tile.Y] = null;
                Release(tile);
            }
        }

        // Groups of chainGroupSize or more that hold a tile that just landed. Each group starts at that tile.
        List<List<TileView>> FindChains(List<TileView> landed)
        {
            var chains = new List<List<TileView>>();
            var checkedTiles = new HashSet<TileView>();
            foreach (TileView tile in landed)
            {
                if (checkedTiles.Contains(tile)) continue;
                List<TileView> group = FindGroup(tile);
                checkedTiles.UnionWith(group);
                if (group.Count >= level.chainGroupSize) chains.Add(group);
            }
            return chains;
        }

        // Moves tiles down into the gaps and drops new ones in from above; every tile that moves goes into landed.
        // Returns how long that takes.
        float Collapse(List<TileView> landed)
        {
            float longest = 0f;
            for (int x = 0; x < Width; x++)
            {
                int write = 0;
                for (int y = 0; y < Height; y++)
                {
                    TileView tile = grid[x, y];
                    if (tile == null) continue;
                    if (y != write)
                    {
                        grid[x, write] = tile;
                        grid[x, y] = null;
                        tile.Y = write;
                        float duration = FallTime(y - write);
                        tile.MoveTo(CellPosition(x, write), duration, 0f);
                        longest = Mathf.Max(longest, duration);
                        landed.Add(tile);
                    }
                    write++;
                }

                for (int y = write; y < Height; y++)
                {
                    TileView tile = Spawn(x, y, RandomColor());
                    float start = Height + (y - write) + 0.2f;
                    tile.Rect.anchoredPosition = CellPosition(x, start);
                    float duration = FallTime(start - y);
                    tile.MoveTo(CellPosition(x, y), duration, 0f);
                    longest = Mathf.Max(longest, duration);
                    landed.Add(tile);
                }
            }
            return longest;
        }

        IEnumerator Shuffle()
        {
            var tiles = new List<TileView>(grid.Length);
            foreach (TileView tile in grid) tiles.Add(tile);
            do
            {
                for (int i = tiles.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
                }
                for (int i = 0; i < tiles.Count; i++)
                {
                    int x = i % Width, y = i / Width;
                    grid[x, y] = tiles[i];
                    tiles[i].X = x;
                    tiles[i].Y = y;
                }
            } while (!HasMove());

            foreach (TileView tile in tiles) tile.MoveTo(CellPosition(tile.X, tile.Y), 0.35f, 0f);
            yield return new WaitForSeconds(0.55f);
        }

        List<TileView> FindGroup(TileView start)
        {
            var group = new List<TileView>();
            var seen = new bool[Width, Height];
            var open = new Stack<TileView>();
            open.Push(start);
            seen[start.X, start.Y] = true;
            while (open.Count > 0)
            {
                TileView tile = open.Pop();
                group.Add(tile);
                foreach ((int dx, int dy) in Neighbours)
                {
                    int nx = tile.X + dx, ny = tile.Y + dy;
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Height || seen[nx, ny]) continue;
                    TileView next = grid[nx, ny];
                    if (next == null || next.ColorIndex != start.ColorIndex) continue;
                    seen[nx, ny] = true;
                    open.Push(next);
                }
            }
            return group;
        }

        bool HasMove()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int color = grid[x, y].ColorIndex;
                    if (x + 1 < Width && grid[x + 1, y].ColorIndex == color) return true;
                    if (y + 1 < Height && grid[x, y + 1].ColorIndex == color) return true;
                }
            }
            return false;
        }

        IEnumerator Unlock(float delay)
        {
            yield return new WaitForSeconds(delay);
            busy = false;
        }

        TileView Spawn(int x, int y, int color)
        {
            TileView tile = pool.Count > 0 ? pool.Pop() : Instantiate(tilePrefab, tileRoot);
            tile.gameObject.SetActive(true);
            tile.Setup(this, color, tileSet);
            tile.Rect.sizeDelta = Vector2.one * tileSize;
            tile.X = x;
            tile.Y = y;
            grid[x, y] = tile;
            return tile;
        }

        void Release(TileView tile)
        {
            tile.StopAllCoroutines();
            tile.gameObject.SetActive(false);
            pool.Push(tile);
        }

        int RandomColor() => Random.Range(0, level.colorCount);

        Vector2 CellPosition(int x, float y) =>
            new Vector2((x - (Width - 1) * 0.5f) * cellSize, (y - (Height - 1) * 0.5f) * cellSize);

        static float FallTime(float cells) => 0.12f + 0.035f * cells;
    }
}
