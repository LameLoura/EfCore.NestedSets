using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EfCore.NestedSets.Tests
{
    [TestClass]
    public class ModulStructureTests
    {
        //public static int TestEntryId = 133;

        private static int lol = 12;
        public static int getTestEntry()
        {
            return ++lol;
        }

        NestedSetManager<AppDbContext, Node, int, int?> _ns;
        NestedSetManager<AppDbContext, ModuleStructure, int, int?> _nodeStrcutManager;
        private AppDbContext _db;
  

        [TestInitialize]
        public void SetUp()
        {
          
            // Clean up from the last test, but do this on set-up not
            // tear-down so it is possible to inspect the database with
            // the results of the last test
            DbSql.RunDbSql("DELETE FROM Nodes");
            _db = new AppDbContext();
            _ns = new NestedSetManager<AppDbContext, Node, int, int?>(_db, d => d.Nodes);
            _nodeStrcutManager =
                new NestedSetManager<AppDbContext, ModuleStructure, int, int?>
                (_db, d => d.ModuleStructures);
        }

        [TestCleanup]
        public void TearDown()
        {
            _db.Dispose();
        }

        [TestMethod]
        public void TestMyOwn()
        {
            //test
            ModuleStructure root = new ModuleStructure { Name = "4 More Ultimate Potatoes!" };
            root = _nodeStrcutManager.InsertRoot(root, ModulStructureTests.getTestEntry(), NestedSetInsertMode.Right);
        }

        private static void AssertDb(int? rootId, params Node[] expectedNodes)
        {
            using (var db = new AppDbContext())
            {
                var nodes = db.Nodes.Where(n => n.RootId == rootId);
                Assert.AreEqual(expectedNodes.Length, nodes.Count());
                for (var i = 0; i < expectedNodes.Length; i++)
                {
                    var node = nodes.SingleOrDefault(n => n.Name == expectedNodes[i].Name);
                    Assert.AreEqual(rootId, node.RootId);
                    Assert.AreEqual(expectedNodes[i].Left, node.Left);
                    Assert.AreEqual(expectedNodes[i].Right, node.Right);
                    Assert.AreEqual(expectedNodes[i].ParentId, node.ParentId);
                    Assert.AreEqual(expectedNodes[i].Level, node.Level);
                }
            }
        }
    }
}