using System.Numerics;
using System.Runtime.CompilerServices;
using Ajiva.Systems.VulcanEngine.Debug;

namespace Ajiva.Components.Transform.SpatialAcceleration;

/*
 * This code is inspired by the work of OneLoneCoder / Javidx9
 * https://github.com/OneLoneCoder/Javidx9/blob/master/PixelGameEngine/SmallerProjects/OneLoneCoder_PGE_QuadTree1.cpp
 * The original code is licensed under the OLC-3 Licence.
 */
public class StaticOctalTree<T, TItem> : IRespectable where TItem : StaticOctalItem<T>
{
    private const int AREAS_PER_LEAF = 8;

    private StaticOctalSpace _area;

    private StaticOctalSpace[] _children = null!;
    private StaticOctalTree<T, TItem>?[] _childrenTrees;
    private int _depth;
    private LinkedList<TItem>? _items;
    private int _maxDepth;

    private bool hasRest;
    private readonly IDebugVisualPool _containerConfig;

    public StaticOctalTree(IDebugVisualPool debugVisualPool)
    {
        _containerConfig = debugVisualPool;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Clear();
        hasRest = true;
    }

    public void Setup(StaticOctalSpace area, int depth, int maxDepth)
    {
        _depth = depth;
        _maxDepth = maxDepth;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_childrenTrees is not null && _childrenTrees.Any(x => x is not null)) Log.Error("already setup");
        _childrenTrees ??= new StaticOctalTree<T, TItem>[AREAS_PER_LEAF]; //ArrayPool<StaticOctalTree<T, TItem>>.Shared.Rent(AREAS_PER_LEAF);
        _items ??= new LinkedList<TItem>(); //todo move to first use
        Resize(area);
        hasRest = false;
    }

    // Force area change on Tree, invalidates this and all child layers
    public void Resize(StaticOctalSpace area)
    {
        Clear();

        _area = area;
        var childSize = _area.Size / 2;

        _children = new StaticOctalSpace[AREAS_PER_LEAF] {
            //Top Left Front
            new StaticOctalSpace(_area.Position, childSize),
            //Top Right Front
            new StaticOctalSpace(new Vector3(_area.Position.X + childSize.X, _area.Position.Y, _area.Position.Z), childSize),
            //Bottom Left Front
            new StaticOctalSpace(new Vector3(_area.Position.X, _area.Position.Y + childSize.Y, _area.Position.Z), childSize),
            //Bottom Right Front
            new StaticOctalSpace(new Vector3(_area.Position.X + childSize.X, _area.Position.Y + childSize.Y, _area.Position.Z), childSize),
            //Top Left Back
            new StaticOctalSpace(new Vector3(_area.Position.X, _area.Position.Y, _area.Position.Z + childSize.Z), childSize),
            //Top Right Back
            new StaticOctalSpace(new Vector3(_area.Position.X + childSize.X, _area.Position.Y, _area.Position.Z + childSize.Z), childSize),
            //Bottom Left Back
            new StaticOctalSpace(new Vector3(_area.Position.X, _area.Position.Y + childSize.Y, _area.Position.Z + childSize.Z), childSize),
            //Bottom Right Back
            new StaticOctalSpace(new Vector3(_area.Position.X + childSize.X, _area.Position.Y + childSize.Y, _area.Position.Z + childSize.Z), childSize)
        };
    }

    public void Clear()
    {
        _items.Clear();

        for (var i = 0; i < AREAS_PER_LEAF; i++)
        {
            if (_childrenTrees[i] is { } item)
            {
                _childrenTrees[i] = null;
                item.Clear();
                Destroy(item);
            }
        }
    }

    public LinkedListNode<TItem> Insert(TItem item)
    {
        for (var i = 0; i < AREAS_PER_LEAF; i++)
        {
            // If the child can wholly contain the item being inserted, insert it there
            if (!_children[i].Contains(item.Space)) continue;

            // Have we reached depth limit?
            if (_depth + 1 >= _maxDepth) continue;

            // No, so does child exist?

            lock (_childrenTrees)
            {
                if (_childrenTrees[i] is null)
                {
                    _childrenTrees[i] = Create(i);
                }

                // Yes, so add item to it
                return _childrenTrees[i]!.Insert(item);
            }
        }

        //ALog.Info($"[Depth: {_depth}]: Inserting item {item.Space} into tree {_area}");
        // It didnt fit, so item must belong to this quad
        lock (_items)
        {
            return _items.AddFirst(item);
        }
    }

    // Returns the objects in the given search area, by adding to supplied list
    public void Search(StaticOctalSpace searchArea, List<TItem> items)
    {
        // First, check for items belonging to this area, add them to the list
        // if there is overlap
        lock (_items)
        {
            foreach (var item in _items)
                if (searchArea.Intersects(item.Space))
                    items.Add(item);
        }

        // Now, check for items belonging to any of the children
        for (var i = 0; i < AREAS_PER_LEAF; i++)
        {
            if (_childrenTrees[i] is null) continue;

            // If child is entirely contained within area, recursively
            // add all of its children, no need to check boundaries
            if (searchArea.Contains(_children[i]))
                _childrenTrees[i]?.Items(items);

            // If child overlaps with area, check if it intersects
            // with area, if so, recursively add all of its children
            else if (searchArea.Intersects(_children[i])) _childrenTrees[i]?.Search(searchArea, items);
        }
    }

    public void Items(List<TItem> items)
    {
        lock (_items)
        {
            items.AddRange(_items);
        }

        for (var i = 0; i < AREAS_PER_LEAF; i++)
            if (_childrenTrees[i] is not null)
                _childrenTrees[i]!.Items(items);
    }

    public LinkedListNode<TItem>? Remove(TItem item)
    {
        for (var i = 0; i < AREAS_PER_LEAF; i++)
        {
            if (!_children[i].Contains(item.Space)) continue;

            // Have we reached depth limit?
            if (_depth + 1 >= _maxDepth) continue;

            // No, so does child exist?
            var staticOctalTree = _childrenTrees[i];
            if (staticOctalTree is null) continue;

            // Yes, so remove item to it
            var removed = staticOctalTree.Remove(item);

            // If the child is empty, remove it
            lock (_childrenTrees)
            {
                if (!staticOctalTree.IsEmpty()) continue;
                _childrenTrees[i] = null;
            }

            Destroy(staticOctalTree);
        }

        // It didnt fit, so item must belong to this quad
        lock (_items)
        {
            var find = _items.Find(item);
            if (find is null) return null;

            _items.Remove(find);
            return find;
        }
    }

    private void Destroy(StaticOctalTree<T, TItem> staticOctalTree)
    {
        _containerConfig.UpdateVisual(this, _area);
        _containerConfig.DestroyVisual(staticOctalTree);
        //ObjectPool<StaticOctalTree<T, TItem>>.Instance.Return(staticOctalTree);
    }

    private StaticOctalTree<T, TItem> Create(int i)
    {
        var created = new StaticOctalTree<T, TItem>(_containerConfig); //ObjectPool<StaticOctalTree<T, TItem>>.Instance.Rent();
        created.Setup(_children[i], _depth + 1, _maxDepth);
        _containerConfig.CreateVisual(created, created._area);
        return created;
    }

    public bool IsEmpty()
    {
        // If this tree has items, it is not empty
        if (_items.Count > 0) return false;

        // If this tree has no items, but has children, it is not empty
        for (var i = 0; i < AREAS_PER_LEAF; i++)
            if (_childrenTrees[i] is not null)
                return false;

        // If this tree has no items, and no children, it is empty
        return true;
    }

    ~StaticOctalTree()
    {
        //ArrayPool<StaticOctalTree<T, TItem>>.Shared.Return(_childrenTrees, true);
        Clear();
    }

    public void Adopt(StaticOctalTree<T, TItem> tree, int oldIndex)
    {
        if (_childrenTrees[oldIndex] is not null) throw new Exception("Cannot adopt tree, child already exists");
        _childrenTrees[oldIndex] = tree;
        _depth = tree._depth + 1;
        _maxDepth = tree._maxDepth;
        tree.Adopted(this);
    }

    private void Adopted(StaticOctalTree<T, TItem> parent)
    {
        _depth = parent._depth + 1;
        _maxDepth = parent._maxDepth;
        if (_depth > _maxDepth)
        {
            Log.Warning("[Depth: {Depth}]: Max depth exceeded, due to adoption of {Tree} into {Parent}", _depth, this, parent);
        }
    }
}
public interface IStaticOctalTreeContainer<T>
{
    bool IsEmpty { get; }
    int Count { get; }
    List<StaticOctalItem<T>> Items { get; }
    StaticOctalItem<T> Insert(StaticOctalItem<T> item);
    StaticOctalItem<T> Insert(T item, StaticOctalSpace space);
    void Remove(StaticOctalItem<T> item);
    List<StaticOctalItem<T>> Search(StaticOctalSpace searchArea);
    void Clear();
    StaticOctalItem<T>? Relocate(StaticOctalItem<T> item, StaticOctalSpace space);
}
public class StaticOctalTreeContainer<T> : IStaticOctalTreeContainer<T>
{
    private readonly LinkedList<LinkedListNode<StaticOctalItem<T>>> _items = new LinkedList<LinkedListNode<StaticOctalItem<T>>>();
    private readonly StaticOctalTree<T, StaticOctalItem<T>> _tree;

    public StaticOctalTreeContainer(StaticOctalSpace area, int maxDepth, IDebugVisualPool debug)
    {
        _tree = new StaticOctalTree<T, StaticOctalItem<T>>(debug);
        _tree.Setup(area, 0, maxDepth);
        Log.Information("Created tree {area} depth 0 Center {2}", area, area.Position + area.Size / 2);
    }

    public bool IsEmpty => _items.Count == 0;
    public int Count => _items.Count;

    public List<StaticOctalItem<T>> Items
    {
        get
        {
            lock (_items)
            {
                return _items.Select(x => x.Value).ToList();
            }
        }
    }

    public StaticOctalItem<T> Insert(StaticOctalItem<T> item)
    {
        var node = _tree.Insert(item);
        lock (_items)
        {
            _items.AddLast(node);
        }
        return node.Value;
    }

    public StaticOctalItem<T> Insert(T item, StaticOctalSpace space)
    {
        return Insert(new StaticOctalItem<T>(item, space));
    }

    public void Remove(StaticOctalItem<T> item)
    {
        var node = _tree.Remove(item);
        if (node is null) return; //todo Indication if removed
        lock (_items)
        {
            _items.Remove(node);
        }
    }

    public List<StaticOctalItem<T>> Search(StaticOctalSpace searchArea)
    {
        var items = new List<StaticOctalItem<T>>();
        //lock (_items)
        {
            _tree.Search(searchArea, items);
        }
        return items;
    }

    public void Clear()
    {
        lock (_items)
        {
            _tree.Clear();
            _items.Clear();
        }
    }

    public StaticOctalItem<T>? Relocate(StaticOctalItem<T> item, StaticOctalSpace space)
    {
        var oldItem = _tree.Remove(item);
        if (oldItem is null) return null;

        LinkedListNode<LinkedListNode<StaticOctalItem<T>>>? found;
        // ReSharper disable once InconsistentlySynchronizedField
        // because we are searching and logging errors, so we don't care if it is not thread safe
        // lock used to consume 50% cpu time
        found = _items.Find(oldItem); //96.4% execution time (52.3% in EqualityComparer<T>.Default.Equals)
        if (found is null)
        {
            Log.Error("Could not find item {item} in list", item);
            return null;
        }
        found.Value = _tree.Insert(item with {
            Space = space
        });
        return found.Value.Value;
    }
}
public readonly struct StaticOctalSpace
{
    public override string ToString()
    {
        return $"{nameof(StaticOctalSpace)} [p:{_position}, s:{_size}]";
    }

    private readonly Vector3 _position;
    private readonly Vector3 _size;

    public StaticOctalSpace(Vector3 position, Vector3 size)
    {
        _position = position;
        _size = size;
    }

    public Vector3 Min => _position;
    public Vector3 Max => _position + _size;

    public Vector3 Position => _position;
    public Vector3 Size => _size;
    public Vector3 Center => _position + _size / 2;
    public static StaticOctalSpace Empty { get; } = new StaticOctalSpace(Vector3.Zero, Vector3.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Vector3 point)
    {
        return point.X >= _position.X && point.X <= _position.X + _size.X &&
               point.Y >= _position.Y && point.Y <= _position.Y + _size.Y &&
               point.Z >= _position.Z && point.Z <= _position.Z + _size.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(StaticOctalSpace space)
    {
        return space.Position.X >= _position.X && space.Position.X + space.Size.X <= _position.X + _size.X &&
               space.Position.Y >= _position.Y && space.Position.Y + space.Size.Y <= _position.Y + _size.Y &&
               space.Position.Z >= _position.Z && space.Position.Z + space.Size.Z <= _position.Z + _size.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(StaticOctalSpace space)
    {
        return space.Position.X + space.Size.X >= _position.X && space.Position.X <= _position.X + _size.X &&
               space.Position.Y + space.Size.Y >= _position.Y && space.Position.Y <= _position.Y + _size.Y &&
               space.Position.Z + space.Size.Z >= _position.Z && space.Position.Z <= _position.Z + _size.Z;
    }
}
public record class StaticOctalItem<TItem>(TItem Item, StaticOctalSpace Space);
public class DynamicOctalTreeContainer<T> : IStaticOctalTreeContainer<T>
{
    private StaticOctalSpace _area;
    private readonly int _maxDepth;
    private readonly IDebugVisualPool _debug;
    private readonly LinkedList<LinkedListNode<StaticOctalItem<T>>> _items = new LinkedList<LinkedListNode<StaticOctalItem<T>>>();
    private StaticOctalTree<T, StaticOctalItem<T>> _tree;

    public DynamicOctalTreeContainer(StaticOctalSpace area, int maxDepth, IDebugVisualPool debug)
    {
        _area = area;
        _maxDepth = maxDepth;
        _debug = debug;
        _tree = new StaticOctalTree<T, StaticOctalItem<T>>(debug);
        _tree.Setup(area, 0, maxDepth);
        _debug.CreateVisual(this,_area);
        Log.Information("Created tree {area} depth 0 Center {2}", area, area.Position + area.Size / 2);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private void CheckAndRelocate(StaticOctalSpace space)
    {
        if (_area.Contains(space)) return;

        //compute optimal area size, we must get closer to the space parameter
        //we can only double the size of the area
        //but we must chose octal space of the original tree as one of the options

        var newSize = _area.Size * 2;
        var newPos = _area.Position;
        var oldIndex = 0;
//Top Left Front = 0,
//Top Right Front    = 1
//Bottom Left Front     = 2
//Bottom Right Front       = 3
//Top Left Back               = 4
//Top Right Back                 = 5
//Bottom Left Back                  = 6
//Bottom Right Back                    = 7

        if (space.Position.X < _area.Position.X)
        {
            oldIndex += 1;
            newPos.X -= _area.Size.X;
        }
        if (space.Position.Y < _area.Position.Y)
        {
            oldIndex += 2;
            newPos.Y -= _area.Size.Y;
        }
        if (space.Position.Z < _area.Position.Z)
        {
            oldIndex += 4;
            newPos.Z -= _area.Size.Z;
        }

        var newTree = new StaticOctalTree<T, StaticOctalItem<T>>(_debug);
        var newArea = new StaticOctalSpace(newPos, newSize);
        newTree.Setup(newArea, 0, _maxDepth);
        newTree.Adopt(_tree, oldIndex);
        _tree = newTree;
        _area = newArea;
        _debug.UpdateVisual(this,_area);
        Log.Information("Relocated tree {area} depth 0 Center {2}, old index {3}", newArea, newArea.Position + newArea.Size / 2, oldIndex);
        CheckAndRelocate(space);
    }

    public bool IsEmpty => _items.Count == 0;
    public int Count => _items.Count;

    public List<StaticOctalItem<T>> Items
    {
        get
        {
            lock (_items)
            {
                return _items.Select(x => x.Value).ToList();
            }
        }
    }

    public StaticOctalItem<T> Insert(StaticOctalItem<T> item)
    {
        CheckAndRelocate(item.Space);
        var node = _tree.Insert(item);
        lock (_items)
        {
            _items.AddLast(node);
        }
        return node.Value;
    }

    public StaticOctalItem<T> Insert(T item, StaticOctalSpace space)
    {
        CheckAndRelocate(space);
        return Insert(new StaticOctalItem<T>(item, space));
    }

    public void Remove(StaticOctalItem<T> item)
    {
        CheckAndRelocate(item.Space);
        var node = _tree.Remove(item);
        if (node is null) return; //todo Indication if removed
        lock (_items)
        {
            _items.Remove(node);
        }
    }

    public List<StaticOctalItem<T>> Search(StaticOctalSpace searchArea)
    {
        var items = new List<StaticOctalItem<T>>();
        //lock (_items)
        {
            _tree.Search(searchArea, items);
        }
        return items;
    }

    public void Clear()
    {
        lock (_items)
        {
            _tree.Clear();
            _items.Clear();
        }
    }

    public StaticOctalItem<T>? Relocate(StaticOctalItem<T> item, StaticOctalSpace space)
    {
        CheckAndRelocate(space);
        var oldItem = _tree.Remove(item);
        if (oldItem is null) return null;

        LinkedListNode<LinkedListNode<StaticOctalItem<T>>>? found;
        // ReSharper disable once InconsistentlySynchronizedField
        // because we are searching and logging errors, so we don't care if it is not thread safe
        // lock used to consume 50% cpu time
        found = _items.Find(oldItem); //96.4% execution time (52.3% in EqualityComparer<T>.Default.Equals)
        if (found is null)
        {
            Log.Error("Could not find item {item} in list", item);
            return null;
        }
        found.Value = _tree.Insert(item with {
            Space = space
        });
        return found.Value.Value;
    }
}
