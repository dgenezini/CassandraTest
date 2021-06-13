using Cassandra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CassandraConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cluster = Cluster.Builder()
                                 .AddContactPoints("cassandra1")
                                 .Build();

            var session = cluster.Connect();

            session.CreateKeyspaceIfNotExists("my_keyspace", new Dictionary<string, string>() {
                { "class", "SimpleStrategy" },
                { "replication_factor", "2" }
            });

            ListKeyspaces(session);

            session.ChangeKeyspace("my_keyspace");

            session.Execute("create table IF NOT EXISTS \"my_keyspace\".\"actors\" (" +
                "actorid uuid, " +
                "name text, " +
                "birthdate date," +
                "primary key (actorid) )");

            await AddNewActor(session);

            ListActorNames(session);
        }

        private static void ListKeyspaces(ISession session)
        {
            var keyspaceNames = session
                            .Execute("SELECT * FROM system_schema.keyspaces")
                            .Select(row => row.GetValue<string>("keyspace_name"));

            Console.WriteLine("Keyspaces found:");

            foreach (var name in keyspaceNames)
            {
                Console.WriteLine("- {0}", name);
            }
        }

        private static void ListActorNames(ISession session)
        {
            var rs = session.Execute("SELECT * FROM actors");

            Console.WriteLine("Actors found:");

            foreach (var row in rs)
            {
                var value = row.GetValue<string>("name");

                Console.WriteLine("- {0}", value);
            }
        }

        private static async Task AddNewActor(ISession session)
        {
            var NewUserStatement = session.Prepare("INSERT INTO actors (actorid, name, birthdate)" +
                            " VALUES (?, ?, ?)");

            var ActorId = Guid.NewGuid();

            var NewUserBindedStatement = NewUserStatement
                .Bind(ActorId, $"Actor {ActorId}", (int)DateTime.Today.Ticks);

            await session.ExecuteAsync(NewUserBindedStatement);
        }
    }
}
