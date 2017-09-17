using DsonTypes;
using System;
using System.Collections.Generic;
using System.Linq;

public class DsonObjectLocator {
    private class FragmentCollection {
		private DsonDocument doc;
        private Dictionary<string, DsonObject> fragments;

        public static FragmentCollection FillFrom(DsonDocument doc) {
			DsonRoot root = doc.Root;
            Dictionary<string, DsonObject> fragments = new Dictionary<string, DsonObject>();
            foreach (DsonObject obj in Utils.SafeArray(root.geometry_library)) {
                fragments[obj.id] = obj;
            }
            foreach (DsonObject obj in Utils.SafeArray(root.modifier_library)) {
                fragments[obj.id] = obj;
            }
            foreach (DsonObject obj in Utils.SafeArray(root.uv_set_library)) {
                fragments[obj.id] = obj;
            }
            foreach (DsonObject obj in Utils.SafeArray(root.node_library)) {
                fragments[obj.id] = obj;
            }
            foreach (DsonObject obj in Utils.SafeArray(root.image_library)) {
                fragments[obj.id] = obj;
            }
			foreach (DsonObject obj in Utils.SafeArray(root.material_library)) {
                fragments[obj.id] = obj;
            }
            return new FragmentCollection(doc, fragments);
        }

        private FragmentCollection(DsonDocument doc, Dictionary<string, DsonObject> fragments) {
			this.doc = doc;
            this.fragments = fragments;
        }

		public DsonDocument Root => doc;

        public DsonObject Locate(string id) {
            fragments.TryGetValue(id, out DsonObject obj);
            return obj;
        }
    }
	
	private readonly ContentFileLocator fileLocator;
    private readonly Dictionary<string, FragmentCollection> documentCache = new Dictionary<string, FragmentCollection>();

    public DsonObjectLocator(ContentFileLocator fileLocator) {
        this.fileLocator = fileLocator;
    }
	
	private FragmentCollection LocateCollection(string documentPath, bool throwIfMissing = true) {
		if (!documentCache.TryGetValue(documentPath, out FragmentCollection fragments)) {
            string contentFile = fileLocator.Locate(documentPath, throwIfMissing);
			if (contentFile == null) {
				return null;
			}
			DsonDocument root = DsonDocument.LoadFromFile(this, contentFile, documentPath);
            fragments = FragmentCollection.FillFrom(root);

            documentCache[documentPath] = fragments;
        }

		return fragments;
	} 
	
	public DsonDocument LocateRoot(string documentPath) {
		FragmentCollection fragments = LocateCollection(documentPath);
		return fragments.Root;
	}
	
	public IEnumerable<DsonDocument> GetAllDocumentsUnderPath(string basePath) {
		return fileLocator.GetAllUnderPath(basePath).Where(str => str.EndsWith(".dsf")).Select(LocateRoot);
	}
	
    public DsonObject Locate(string url, bool throwIfMissing = true) {
        Uri uri = new Uri("scheme:" + url);
        string documentPath = Uri.UnescapeDataString(uri.AbsolutePath);
		FragmentCollection fragments = LocateCollection(documentPath, throwIfMissing);
		if (fragments == null) {
			return null;
		}

        string fragmentId = Uri.UnescapeDataString(uri.Fragment.Substring(1));
        DsonObject obj = fragments.Locate(fragmentId);
        if (obj == null && throwIfMissing) {
			throw new InvalidOperationException("Couldn't locate fragment in document: " + url);
        }
		return obj;
    }

	public IEnumerable<Modifier> FindAllModifiers(string parent) {
		throw new NotImplementedException();
	}

	internal List<Formula> FindFormulasByOutput(string channelUrl) {
		throw new NotImplementedException();
	}

	internal IEnumerable<Node> FindNodesByParent(string parentUrl) {
		throw new NotImplementedException();
	}
}
