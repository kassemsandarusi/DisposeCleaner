using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Commons;
using Commons.Data;
using Commons.Data.NHibernate;
using Exs.Enrollment;
using Exs.Enrollment.Model;
using Integration.Commons.Images;
using Va.Commons.Vehicle.Images;
using Vehicle.Inventory;

namespace ImageImporter.Importers.WebImporters
{
    class HarvesterProcessor : PhotoProcessor
    {
        private static readonly Logger Log = new Logger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public HarvesterProcessor(PhotoImportConfig config)
            : base(config)
        {
        }

        protected override double MaxErrorPercent { get { return 80; } }

        protected override IEnumerable<PhotoImportWorkItem> GetWorkItems()
        {
            using(InventoryService invService = new InventoryService())
            foreach (InventoryVehicle vehicle in invService.GetVehiclesWithoutUserAddedPhotos(Config.Dealer))
            {
                List<string> uris = GetImageUris(vehicle);
                Log.TraceFormat("Found {0} images for {1}", uris.Count, vehicle.Vin);
                if (uris.Count > 0 && (vehicle.MarketingImageSet == null || uris.Count != vehicle.MarketingImageSet.ImageCount))
                    yield return new PhotoImportWorkItem()
                    {
                        UrlsToDownload = uris,
                        Vehicle = vehicle
                    };
            }
        }

        protected override void SetDefaultOverlays(VehicleImageCollection collection, Entity dealer)
        {
           // we don't set overlays from here   
        }

        private const string GetImageUrisSql = @"
            select v.vin,
                   v.ImageCount imageCount,
                   v.providerid,
                   CAST(COLLECT(urls.url order by urls.position asc) as Commons.Varchars) as ImageUris
              from VehicleCache.VehiclesView v
                   join VEHICLECACHE.VEHICLEIMAGEURLS urls on v.id = urls.vehicleid
                   join radar.dataproviders p on p.id = v.providerid
                   join VehicleCache.SellerDealerMap sellerMap 
                        on sellerMap.sellerid = v.sellerid 
                       and sellerMap.dealerId = :entityId
                       and nvl(sellerMap.overridetypeid,0) < 2 /*don't get negative overrides*/
             where v.vin = :vin
             group by v.vin, v.providerid, v.imageCount, v.sellerid, nvl(sellerMap.overridetypeid,0), datapriority   
             order by nvl(sellerMap.overridetypeid,0) desc, p.datapriority asc, v.imagecount";

        private List<string> GetImageUris(InventoryVehicle vehicle)
        {
            using (XDataTable table = DataSetHelper.GetDataTable(GetImageUrisSql, SessionManager.Instance.GetConnection("va2"),
                new NameValue("vin", vehicle.Vin), new NameValue("entityId", vehicle.Dealer.Id)))
            {
                if (table.Rows.Count == 0)
                    return new List<string>();
                return ((XDataRow)table.Rows[0]).GetList<string>("ImageUris");
            }
        }
    }
}
