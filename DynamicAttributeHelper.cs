
public static class DynamicAttributeHelper
    {
        class PropertyOverridingTypeDescriptor : CustomTypeDescriptor
        {
            private readonly Dictionary<string, PropertyDescriptor> _overridePds = new Dictionary<string, PropertyDescriptor>();
            public PropertyOverridingTypeDescriptor(ICustomTypeDescriptor parent)
                : base(parent)
            { }
            public void OverrideProperty(PropertyDescriptor pd)
            {
                _overridePds[pd.Name] = pd;
            }
            public override object GetPropertyOwner(PropertyDescriptor pd)
            {
                var owner = base.GetPropertyOwner(pd);
                return owner ?? this;
            }
            public PropertyDescriptorCollection GetPropertiesImpl(PropertyDescriptorCollection pdc)
            {
                var propertyDescriptors = new List<PropertyDescriptor>(pdc.Count + 1);
                foreach (PropertyDescriptor pd in pdc)
                {
                    propertyDescriptors.Add(_overridePds.ContainsKey(pd.Name) ? _overridePds[pd.Name] : pd);
                }
                return new PropertyDescriptorCollection(propertyDescriptors.ToArray());
            }
            public override PropertyDescriptorCollection GetProperties()
            {
                return GetPropertiesImpl(base.GetProperties());
            }
            public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            {
                return GetPropertiesImpl(base.GetProperties(attributes));
            }
        }
        class TypeDescriptorOverridingProvider : TypeDescriptionProvider
        {
            private readonly ICustomTypeDescriptor _customTypeDescriptor;
            public TypeDescriptorOverridingProvider(ICustomTypeDescriptor customTypeDescriptor)
            {
                _customTypeDescriptor = customTypeDescriptor;
            }
            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
            {
                return _customTypeDescriptor;
            }
        }
        public static void AddAtrributeToProperty<T>(Expression<Func<T>> selector, object instance, params Attribute[] attributes)
        {
            if (!(selector.Body is MemberExpression memberExpression))
            {
                throw new InvalidOperationException();
            }
            var propertyName = memberExpression.Member.Name;
            var ctd = new PropertyOverridingTypeDescriptor(TypeDescriptor.GetProvider(instance).GetTypeDescriptor(instance));
            var propertyDescriptor = TypeDescriptor.GetProperties(instance).Find(propertyName, true);
            foreach (var attribute in attributes)
            {
                var descriptor = TypeDescriptor.CreateProperty(
                    instance.GetType(),
                    propertyDescriptor,
                    attribute
                );
                ctd.OverrideProperty(descriptor);
            }
            TypeDescriptor.AddProvider(new TypeDescriptorOverridingProvider(ctd), instance);
        }
    }