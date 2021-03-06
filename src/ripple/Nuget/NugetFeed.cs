using System.Collections.Generic;
using System.Linq;
using System.Net;
using FubuCore;
using NuGet;
using ripple.Model;

namespace ripple.Nuget
{
    public class NugetFeed : NugetFeedBase
    {
        private readonly IPackageRepository _repository;
        private readonly string _url;
        private readonly NugetStability _stability;

        public NugetFeed(string url, NugetStability stability)
        {
            _url = url;
            _stability = stability;
            _repository = new PackageRepositoryFactory().CreateRepository(_url);
        }

        public string Url
        {
            get { return _url; }
        }

        public override bool IsOnline()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead(_url))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        protected override IRemoteNuget find(Dependency query)
		{
			SemanticVersion version;
			if (!SemanticVersion.TryParse(query.Version, out version))
			{
				RippleLog.Debug("Could not find exact for " + query);
				return null;
			}

            var versionSpec = new VersionSpec(version);
            var package = _repository.FindPackages(query.Name, versionSpec, query.DetermineStability(_stability) == NugetStability.Anything, true).SingleOrDefault();

            if (package == null)
            {
	            return null;
            }
            
            return new RemoteNuget(package);
        }

        public override IEnumerable<IRemoteNuget> FindLatestByName(string idPart)
        {
            return _repository.GetPackages()
                .Where(package => package.Id.Contains(idPart) && package.IsLatestVersion)
                .ToArray()
                .Select(package => new RemoteNuget(package));
        }

        protected override IRemoteNuget findLatest(Dependency query)
        {
			RippleLog.Debug("Searching for {0} from {1}".ToFormat(query, _url));
            var candidates = _repository.Search(query.Name, query.DetermineStability(_stability) == NugetStability.Anything)
                                        .Where(x => query.Name == x.Id).OrderBy(x => x.Id).ToList();

            var candidate = candidates.FirstOrDefault(x => x.IsAbsoluteLatestVersion)
                            ?? candidates.FirstOrDefault(x => x.IsLatestVersion);

            if (candidate == null)
            {
	            return null;
            }

            return new RemoteNuget(candidate);
        }

		public override IPackageRepository Repository { get { return _repository; } }

        public override string ToString()
        {
            return _url;
        }
    }
}