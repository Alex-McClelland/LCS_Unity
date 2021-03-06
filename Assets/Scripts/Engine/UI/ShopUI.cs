using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Components.World;

namespace LCS.Engine.UI
{
    public interface ShopUI : UIBase
    {
        void init(ShopActions shopActions);
        void startShopping(Entity location, LiberalCrimeSquad.Squad squad);
    }

    public class ShopActions
    {
        public EntityAction buy;
        public EntityAction sell;
        public TwoEntityAction addItemToBuyCart;
        public TwoEntityAction addItemToSellCart;
        public ThreeEntityAction addAllSimilarItemsToSellCart;
        public TwoEntityAction removeItemFromBuyCart;
        public TwoEntityAction removeAllSimilarItemsFromBuyCart;
        public TwoEntityAction removeItemFromSellCart;
        public TwoEntityAction removeAllSimilarItemsFromSellCart;
        public EntityAction finishShopping;

        public delegate void EntityAction(Entity e1);
        public delegate void TwoEntityAction(Entity e1, Entity e2);
        public delegate void ThreeEntityAction(Entity e1, Entity e2, Entity e3);
    }
}
