using VersionOne.SDK.APIClient;

namespace VersionOne.Provisioning.Tests {
    class TestAssetType : IAssetType {
        public bool Is(IAssetType targettype) {
            throw new System.NotImplementedException();
        }

        public IAttributeDefinition GetAttributeDefinition(string name) {
            throw new System.NotImplementedException();
        }

        public bool TryGetAttributeDefinition(string name, out IAttributeDefinition def) {
            throw new System.NotImplementedException();
        }

        public IOperation GetOperation(string name) {
            throw new System.NotImplementedException();
        }

        public bool TryGetOperation(string name, out IOperation op) {
            throw new System.NotImplementedException();
        }

        public string Token {
            get { return "Member"; }
        }

        public IAssetType Base {
            get { throw new System.NotImplementedException(); }
        }

        public string DisplayName {
            get { throw new System.NotImplementedException(); }
        }

        public IAttributeDefinition DefaultOrderBy {
            get { throw new System.NotImplementedException(); }
        }

        public IAttributeDefinition ShortNameAttribute {
            get { throw new System.NotImplementedException(); }
        }

        public IAttributeDefinition NameAttribute {
            get { throw new System.NotImplementedException(); }
        }

        public IAttributeDefinition DescriptionAttribute {
            get { throw new System.NotImplementedException(); }
        }
    }
}
