using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Ограниченный кэшируемый A* для погони за углами. Не управляет агрессией или телом.</summary>
    internal sealed class EnemyChasePath2D
    {
        private const float Cell = .6f;
        private const int MaxCells = 4096;
        private const int MaxExpanded = 1536;
        private readonly List<Vector2> path = new List<Vector2>();
        private readonly List<int> open = new List<int>();
        private float[] cost;
        private float[] score;
        private int[] parent;
        private byte[] state;
        private byte[] walk;
        private Vector2 gridMin;
        private Vector2 lastGoal;
        private int width;
        private int height;
        private int cursor;
        private float nextRepath;
        private readonly System.Random random = new System.Random(System.Guid.NewGuid().GetHashCode());
        private static readonly Unity.Profiling.ProfilerMarker marker = new Unity.Profiling.ProfilerMarker("EmergencyShift.Navigation.Build");
        public int WaypointCount => Mathf.Max(0, path.Count - cursor);

        public Vector2 Direction(Vector2 position, Vector2 goal, float radius, LayerMask solids)
        {
            Vector2 delta = goal - position;
            if (delta.magnitude < .12f)
            {
                return Vector2.zero;
            }

            if (Clear(position, goal, radius, solids))
            {
                path.Clear();
                cursor = 0;
                return delta.normalized;
            }

            if (Time.time >= nextRepath && NavigationWorkBudget2D.TryReserve())
            {
                using (marker.Auto())
                {
                    Build(position, goal, radius + .02f, solids);
                }

                lastGoal = goal;
                nextRepath = Time.time + .65f + (float)random.NextDouble() * .12f;
            }

            if ((goal - lastGoal).sqrMagnitude > 4)
            {
                path.Clear();
                nextRepath = 0;
                return Vector2.zero;
            }

            while (cursor < path.Count && Vector2.Distance(position, path[cursor]) < .15f)
            {
                cursor++;
            }

            if (cursor >= path.Count)
            {
                return Vector2.zero;
            }

            // Срезаем доступные промежуточные клетки, чтобы тело не шло ступеньками.
            for (int i = path.Count - 1; i > cursor; i--)
            {
                if (Clear(position, path[i], radius + .01f, solids))
                {
                    cursor = i;
                    break;
                }
            }

            Vector2 toNode = path[cursor] - position;
            float step = Mathf.Min(.3f, toNode.magnitude);
            if (Physics2D.CircleCast(position, radius, toNode.normalized, step, solids).collider != null)
            {
                path.Clear();
                return Vector2.zero;
            }

            return toNode.normalized;
        }

        private void Build(Vector2 start, Vector2 goal, float radius, LayerMask solids)
        {
            path.Clear();
            open.Clear();
            cursor = 0;
            gridMin = new Vector2(Mathf.Floor((Mathf.Min(start.x, goal.x) - 4) / Cell) * Cell, Mathf.Floor((Mathf.Min(start.y, goal.y) - 4) / Cell) * Cell);
            width = Mathf.CeilToInt((Mathf.Max(start.x, goal.x) + 4 - gridMin.x) / Cell) + 1;
            height = Mathf.CeilToInt((Mathf.Max(start.y, goal.y) + 4 - gridMin.y) / Cell) + 1;
            int count = width * height;
            if (count > MaxCells)
            {
                return; // Дальний маршрут ждёт новой попытки, без неограниченного поиска.
            }

            if (cost == null)
            {
                cost = new float[MaxCells];
                score = new float[MaxCells];
                parent = new int[MaxCells];
                state = new byte[MaxCells];
                walk = new byte[MaxCells];
            }

            for (int i = 0; i < count; i++)
            {
                state[i] = 0;
                walk[i] = 0;
                parent[i] = -1;
            }

            int first = Nearest(start, radius, solids, true), last = Nearest(goal, radius, solids, false);
            if (first < 0 || last < 0)
            {
                return;
            }

            cost[first] = 0;
            score[first] = Heuristic(first, last);
            state[first] = 1;
            open.Add(first);
            for (int expanded = 0; expanded < MaxExpanded && open.Count > 0; expanded++)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                {
                    if (score[open[i]] < score[open[best]])
                    {
                        best = i;
                    }
                }

                int current = open[best];
                open[best] = open[open.Count - 1];
                open.RemoveAt(open.Count - 1);
                if (current == last)
                {
                    for (int node = last; node >= 0; node = parent[node])
                    {
                        path.Add(Point(node));
                    }

                    path.Reverse();
                    return;
                }

                state[current] = 2;
                int x = current % width, y = current / width;
                if (x > 0)
                {
                    Visit(current, current - 1, last, radius, solids);
                }

                if (x + 1 < width)
                {
                    Visit(current, current + 1, last, radius, solids);
                }

                if (y > 0)
                {
                    Visit(current, current - width, last, radius, solids);
                }

                if (y + 1 < height)
                {
                    Visit(current, current + width, last, radius, solids);
                }
            }
        }

        private void Visit(int from, int to, int last, float radius, LayerMask solids)
        {
            if (state[to] == 2 || !Walkable(to, radius, solids))
            {
                return;
            }

            float candidate = cost[from] + 1;
            if (state[to] == 1 && cost[to] <= candidate)
            {
                return;
            }

            if (!Clear(Point(from), Point(to), radius, solids))
            {
                return;
            }

            parent[to] = from;
            cost[to] = candidate;
            score[to] = candidate + Heuristic(to, last);
            if (state[to] == 0)
            {
                state[to] = 1;
                open.Add(to);
            }
        }

        private int Nearest(Vector2 point, float radius, LayerMask solids, bool start)
        {
            int x = Mathf.RoundToInt((point.x - gridMin.x) / Cell), y = Mathf.RoundToInt((point.y - gridMin.y) / Cell);
            int best = -1;
            float distance = float.MaxValue;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    {
                        continue;
                    }

                    int node = ny * width + nx;
                    if (!Walkable(node, radius, solids))
                    {
                        continue;
                    }

                    Vector2 candidate = Point(node);
                    float d = (candidate - point).sqrMagnitude;
                    if (d >= distance)
                    {
                        continue;
                    }

                    // Игрок может прижаться к стене ближе, чем помещается широкое тело сталкера.
                    // Тело может быть ближе к углу, чем запас .02 у сетки. Начальный выход
                    // проверяем фактическим радиусом, иначе безопасный маршрут становится недоступен.
                    if (start ? !Clear(point, candidate, Mathf.Max(.01f, radius - .02f), solids) : Physics2D.Linecast(point, candidate, solids).collider != null)
                    {
                        continue;
                    }

                    best = node;
                    distance = d;
                }
            }

            return best;
        }

        private bool Walkable(int node, float radius, LayerMask solids)
        {
            if (walk[node] == 0)
            {
                walk[node] = (byte)(Physics2D.OverlapCircle(Point(node), radius, solids) == null ? 1 : 2);
            }

            return walk[node] == 1;
        }

        private Vector2 Point(int node) => gridMin + new Vector2(node % width, node / width) * Cell;
        private float Heuristic(int a, int b) => Mathf.Abs(a % width - b % width) + Mathf.Abs(a / width - b / width);
        private static bool Clear(Vector2 a, Vector2 b, float radius, LayerMask solids)
        {
            Vector2 delta = b - a;
            return delta.sqrMagnitude < .00001f || Physics2D.CircleCast(a, radius, delta.normalized, delta.magnitude, solids).collider == null;
        }
    }
}
