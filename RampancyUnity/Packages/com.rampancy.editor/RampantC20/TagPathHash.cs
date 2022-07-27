using Faithlife.Utility;
using RampantC20;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rampancy
{
    // Get guid hashes from a tag reltive path, this way they are dertimistic across users install so sharing maps and prefabs should just work
    public static class TagPathHash
    {
        public static readonly Guid Halo1MccNamespace     = new Guid("b4c1133d-3953-4368-bb48-288b220f3412");
        public static readonly Guid Halo2MccNamespace     = new Guid("16973657-1e15-4a40-bbd5-f1da0c97fc5b");
        public static readonly Guid Halo3MccNamespace     = new Guid("43185f76-3001-44b3-8d1e-7468805b2bda");
        public static readonly Guid Halo3ODSTMccNamespace = new Guid("2e9490a9-27be-4c8b-a054-7386f4b386d5");

        private const int HASH_VERSION = 5; // SHA-1

        public static string GetHash(string path, GameVersions version)
        {
            var guid = version switch
            {
                GameVersions.Halo1Mcc  => H1MccPathHash(path),
                //GameVersions.Halo2Mcc  => H2MccPathHash(path),
                GameVersions.Halo3     => H3MccPathHash(path),
                GameVersions.Halo3ODST => H3MccODSTPathHash(path),
                _                      => string.Empty
            };

            return guid;
        }

        public static string GetPathHash(Guid nameSpace, string path) => GuidUtility.Create(nameSpace, path.Replace("/", "\\"), HASH_VERSION).ToString("N");
        public static string H1MccPathHash(string path)               => GetPathHash(Halo1MccNamespace, path);
        public static string H2MccPathHash(string path)               => GetPathHash(Halo2MccNamespace, path);
        public static string H3MccPathHash(string path)               => GetPathHash(Halo3MccNamespace, path);
        public static string H3MccODSTPathHash(string path)           => GetPathHash(Halo3ODSTMccNamespace, path);
    }
}
