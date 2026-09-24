using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Records the return of an asset from an employee/guard/branch.
    /// Linked to the original AssetAllocation that is being closed.
    /// On save: AssetAllocation.IsActive → false, Asset.AssetStatus → Available,
    ///          movement history row created.
    /// </summary>
    [Table("AssetReturn")]
    public class AssetReturn
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated, e.g. AR0001</summary>
        public string ReturnNo { get; set; } = string.Empty;

        public int AssetID { get; set; }

        /// <summary>FK to the allocation being closed by this return</summary>
        public int AllocationID { get; set; }

        public DateTime ReturnDate { get; set; }

        /// <summary>Branch | Staff | Guard — who is returning the asset</summary>
        public string ReturnedByType { get; set; } = string.Empty;
        public int? ReturnedByID { get; set; }
        public string? ReturnedByName { get; set; }

        /// <summary>Good | Fair | Poor | Damaged</summary>
        public string? ReturnCondition { get; set; }

        public string? ReceivedBy { get; set; }
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        [ForeignKey("AssetID")]
        public virtual AssetMaster? Asset { get; set; }

        [ForeignKey("AllocationID")]
        public virtual AssetAllocation? Allocation { get; set; }
    }
}
