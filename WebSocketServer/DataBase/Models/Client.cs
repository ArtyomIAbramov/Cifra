using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.DataBase.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string SubProtocol { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}
