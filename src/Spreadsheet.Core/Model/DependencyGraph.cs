namespace Spreadsheet.Core.Model;

/// <summary>
/// Граф залежностей клітинок: клітинка → її посилання та зворотні зв'язки.
/// Обходи ітеративні (без рекурсії). Порядок обчислення знаходить алгоритм Тарьяна
/// (компоненти сильної зв'язності) з подальшим топологічним сортуванням компонентів.
/// </summary>
internal sealed class DependencyGraph
{
    private static readonly IComparer<CellAddress> AddressOrder = Comparer<CellAddress>.Create(
        (a, b) => a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Column.CompareTo(b.Column));

    private readonly Dictionary<CellAddress, CellAddress[]> _dependencies = new();
    private readonly Dictionary<CellAddress, HashSet<CellAddress>> _dependents = new();

    /// <summary>Встановлює (замінює) посилання клітинки.</summary>
    public void SetDependencies(CellAddress cell, IEnumerable<CellAddress> dependencies)
    {
        ClearDependencies(cell);
        CellAddress[] list = dependencies.Distinct().ToArray();
        if (list.Length == 0)
        {
            return;
        }

        _dependencies[cell] = list;
        foreach (CellAddress dependency in list)
        {
            if (!_dependents.TryGetValue(dependency, out HashSet<CellAddress>? set))
            {
                set = new HashSet<CellAddress>();
                _dependents[dependency] = set;
            }

            set.Add(cell);
        }
    }

    /// <summary>Очищає посилання клітинки.</summary>
    public void ClearDependencies(CellAddress cell)
    {
        if (!_dependencies.Remove(cell, out CellAddress[]? old))
        {
            return;
        }

        foreach (CellAddress dependency in old)
        {
            if (_dependents.TryGetValue(dependency, out HashSet<CellAddress>? set))
            {
                set.Remove(cell);
                if (set.Count == 0)
                {
                    _dependents.Remove(dependency);
                }
            }
        }
    }

    /// <summary>Усі клітинки, що транзитивно залежать від заданої (сама вона — лише якщо лежить на циклі).</summary>
    public IReadOnlyCollection<CellAddress> GetDependents(CellAddress cell) => Reach([cell]);

    /// <summary>
    /// Будує план обчислення для початкових клітинок та всіх транзитивно залежних:
    /// топологічний порядок (між незалежними — за рядком, потім стовпцем) і клітинки циклів.
    /// </summary>
    public EvaluationPlan Plan(IEnumerable<CellAddress> seeds)
    {
        var seedList = seeds.ToList();
        var affected = new HashSet<CellAddress>(Reach(seedList));
        affected.UnionWith(seedList);

        List<CellAddress> sorted = affected.ToList();
        sorted.Sort(AddressOrder);

        List<List<CellAddress>> components = FindComponents(sorted, affected, out var componentOf);

        var cyclic = new HashSet<CellAddress>();
        foreach (List<CellAddress> component in components)
        {
            if (component.Count > 1)
            {
                cyclic.UnionWith(component);
            }
            else if (_dependencies.TryGetValue(component[0], out CellAddress[]? deps) && deps.Contains(component[0]))
            {
                cyclic.Add(component[0]);
            }
        }

        return new EvaluationPlan(OrderComponents(components, componentOf, affected), cyclic);
    }

    private HashSet<CellAddress> Reach(IEnumerable<CellAddress> starts)
    {
        var result = new HashSet<CellAddress>();
        var queue = new Queue<CellAddress>(starts);
        while (queue.Count > 0)
        {
            CellAddress current = queue.Dequeue();
            if (!_dependents.TryGetValue(current, out HashSet<CellAddress>? set))
            {
                continue;
            }

            foreach (CellAddress dependent in set)
            {
                if (result.Add(dependent))
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        return result;
    }

    private CellAddress[] Neighbors(CellAddress cell, HashSet<CellAddress> affected) =>
        _dependencies.TryGetValue(cell, out CellAddress[]? deps)
            ? deps.Where(affected.Contains).ToArray()
            : [];

    private List<List<CellAddress>> FindComponents(
        List<CellAddress> sortedRoots,
        HashSet<CellAddress> affected,
        out Dictionary<CellAddress, int> componentOf)
    {
        var components = new List<List<CellAddress>>();
        componentOf = new Dictionary<CellAddress, int>();
        var index = new Dictionary<CellAddress, int>();
        var low = new Dictionary<CellAddress, int>();
        var onStack = new HashSet<CellAddress>();
        var stack = new Stack<CellAddress>();
        var frames = new Stack<Frame>();
        int counter = 0;

        void Visit(CellAddress node)
        {
            index[node] = low[node] = counter++;
            stack.Push(node);
            onStack.Add(node);
            frames.Push(new Frame(node, Neighbors(node, affected)));
        }

        foreach (CellAddress root in sortedRoots)
        {
            if (index.ContainsKey(root))
            {
                continue;
            }

            Visit(root);
            while (frames.Count > 0)
            {
                Frame frame = frames.Peek();
                if (frame.Next < frame.Neighbors.Length)
                {
                    CellAddress next = frame.Neighbors[frame.Next++];
                    if (!index.ContainsKey(next))
                    {
                        Visit(next);
                    }
                    else if (onStack.Contains(next))
                    {
                        low[frame.Node] = Math.Min(low[frame.Node], index[next]);
                    }

                    continue;
                }

                frames.Pop();
                CellAddress node = frame.Node;
                if (low[node] == index[node])
                {
                    var component = new List<CellAddress>();
                    CellAddress member;
                    do
                    {
                        member = stack.Pop();
                        onStack.Remove(member);
                        componentOf[member] = components.Count;
                        component.Add(member);
                    }
                    while (!member.Equals(node));

                    component.Sort(AddressOrder);
                    components.Add(component);
                }

                if (frames.Count > 0)
                {
                    CellAddress parent = frames.Peek().Node;
                    low[parent] = Math.Min(low[parent], low[node]);
                }
            }
        }

        return components;
    }

    private List<CellAddress> OrderComponents(
        List<List<CellAddress>> components,
        Dictionary<CellAddress, int> componentOf,
        HashSet<CellAddress> affected)
    {
        int count = components.Count;
        var pending = new int[count];
        var successors = new List<int>[count];
        for (int c = 0; c < count; c++)
        {
            successors[c] = new List<int>();
        }

        for (int c = 0; c < count; c++)
        {
            var needed = new HashSet<int>();
            foreach (CellAddress cell in components[c])
            {
                foreach (CellAddress dependency in Neighbors(cell, affected))
                {
                    int other = componentOf[dependency];
                    if (other != c)
                    {
                        needed.Add(other);
                    }
                }
            }

            pending[c] = needed.Count;
            foreach (int other in needed)
            {
                successors[other].Add(c);
            }
        }

        // Готові компоненти беруться за найменшою адресою — порядок детермінований.
        var ready = new SortedSet<(CellAddress First, int Id)>(
            Comparer<(CellAddress First, int Id)>.Create((a, b) =>
            {
                int byAddress = AddressOrder.Compare(a.First, b.First);
                return byAddress != 0 ? byAddress : a.Id.CompareTo(b.Id);
            }));

        for (int c = 0; c < count; c++)
        {
            if (pending[c] == 0)
            {
                ready.Add((components[c][0], c));
            }
        }

        var order = new List<CellAddress>(affected.Count);
        while (ready.Count > 0)
        {
            (CellAddress _, int id) = ready.Min;
            ready.Remove(ready.Min);
            order.AddRange(components[id]);
            foreach (int successor in successors[id])
            {
                if (--pending[successor] == 0)
                {
                    ready.Add((components[successor][0], successor));
                }
            }
        }

        return order;
    }

    private sealed class Frame(CellAddress node, CellAddress[] neighbors)
    {
        public CellAddress Node { get; } = node;

        public CellAddress[] Neighbors { get; } = neighbors;

        public int Next { get; set; }
    }
}
