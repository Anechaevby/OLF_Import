using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WFProcessImport.Activities
{
    public class BaseCodeActivity : CodeActivity
    {
        protected List<LocationReference> _locationReferences;

        protected override void Execute (CodeActivityContext context)
        {
            var pi = context.GetType().GetProperty("AllowChainedEnvironmentAccess", BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi != null)
            {
                var pivalue = (bool) pi.GetValue(context, null);
                if (!pivalue)
                {
                    pi.SetValue(context, true, null);
                }
            }
        }

        protected T GetVariableFromContext<T> (string varname, CodeActivityContext context)
        {   // *** Get value *** //
            if (this._locationReferences.Any())
            {
                if (this._locationReferences.Find(x => x.Name.Equals(varname) && x.Type == typeof(T)) is LocationReference locationReference)
                {
                    Location location = locationReference.GetLocation(context);
                    return (T) location.Value;
                }
            }
            return default(T);
        }

        protected void SetVariableToContext<T> (string varname, T varvalue, CodeActivityContext context)
        {   // *** Set value *** //
            if (this._locationReferences.Any())
            {
                var locReference = this._locationReferences.Find(x => x.Name.Equals (varname) && x.Type == typeof (T));
                if (locReference?.GetLocation (context) is Location location)
                {
                    location.Value = varvalue;
                }
            }
        }

        protected override void CacheMetadata (CodeActivityMetadata metadata)
        {
            base.CacheMetadata (metadata);

            var environment = metadata.Environment;
            this._locationReferences = new List<LocationReference> ();
            do
            {
                var templistenv = environment.GetLocationReferences ().ToList ();
                if (templistenv.Any())
                {
                    this._locationReferences.AddRange (templistenv);
                }
                environment = environment.Parent;
            } while (environment != null);
        }
    }
}
