using System.Runtime.Serialization;

namespace DeviceControlWebApp.Controllers
{
    internal class VariableResponse<T>
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public T result { get; set; }
    }
}