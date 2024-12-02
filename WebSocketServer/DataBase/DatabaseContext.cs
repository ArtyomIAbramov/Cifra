using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.DataBase
{
    public class DatabaseContext
    {
        private static DatabaseContext _instance;
        private static readonly object _lock = new object();
        private AppDbContext _context;

        private DatabaseContext()
        {
            using (_context = new AppDbContext())
            {
                _context.Database.EnsureCreated();
            }
        }

        public static DatabaseContext Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new DatabaseContext();
                    }
                    return _instance;
                }
            }
        }

        public AppDbContext GetContext()
        {
            return _context;
        }
    }

}
