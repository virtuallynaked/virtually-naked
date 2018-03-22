using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RandomProvider {
	private static Random seedSource = new Random();

	public static Random Provide() {
		lock(seedSource) {
			int seed = seedSource.Next();
			return new Random(seed);
		}
	}
}
