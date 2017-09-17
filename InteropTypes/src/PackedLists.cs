using ProtoBuf;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

[ProtoContract(UseProtoMembersOnly = true)]
public class PackedLists<T> {
	public static readonly PackedLists<T> Empty = new PackedLists<T>(new ArraySegment[0], new T[0]);

	public static PackedLists<T> MakeEmptyLists(int count) {
		ArraySegment[] segments = new ArraySegment[count];
		T[] elems = new T[0];
		return new PackedLists<T>(segments, elems);
	}

	public static PackedLists<T> Pack(List<List<T>> lists) {
		List<ArraySegment> segments = new List<ArraySegment>();
		List<T> elems = new List<T>();
		foreach (List<T> list in lists) {
			segments.Add(new ArraySegment(elems.Count, list.Count));
			elems.AddRange(list);
		}
		return new PackedLists<T>(segments.ToArray(), elems.ToArray());
	}
	
	public static PackedLists<T> Concat(PackedLists<T> lists1, PackedLists<T> lists2) {
		int baseOffset = lists1.Elems.Length;
		ArraySegment[] segments = lists1.Segments.Concat(lists2.Segments.Select(segment => new ArraySegment(baseOffset + segment.Offset, segment.Count))).ToArray();
		T[] elems = lists1.Elems.Concat(lists2.Elems).ToArray();
		return new PackedLists<T>(segments, elems);
	}

	[ProtoMember(1)]
	public ArraySegment[] Segments { get; private set; }

	[ProtoMember(2)]
	public T[] Elems { get; private set; }

	public int Count => Segments.Length;
	
	public PackedLists(ArraySegment[] segments, T[] elems) {
		Segments = segments;
		Elems = elems;
	}

	private PackedLists() {
		//for protobuf-net
	}

	public IEnumerable<T> GetElements(int listIdx) {
		ArraySegment segment = Segments[listIdx];
		return Enumerable.Range(segment.Offset, segment.Count).Select(i => Elems[i]);
	}

	public PackedLists<U> Map<U>(Func<T, U> func) {
		return new PackedLists<U>(Segments, Elems.Select(func).ToArray());
	}
}
