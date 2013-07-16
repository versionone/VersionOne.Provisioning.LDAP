using VersionOne.SDK.APIClient;

namespace VersionOne.Provisioning {
    public class V1Instance {
        private readonly IServices services;
        private readonly IMetaModel model;
        private readonly string defaultRole;

        public V1Instance(IServices services, IMetaModel model, string defaultRole) {
            this.services = services;
            this.model = model;
            this.defaultRole = defaultRole;
        }

        public IServices Services {
            get { return services; }
        }

        public IMetaModel Model {
            get { return model; }
        }

        public string DefaultRole {
            get { return defaultRole; }
        }
    }
}