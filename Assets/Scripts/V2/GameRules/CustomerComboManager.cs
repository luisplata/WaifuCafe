using System;
using UnityEngine;

public class CustomerComboManager : MonoBehaviour, ICustomComboManager
{
    private IComboManager _comboManager;
    private ComboData currentComboData;
    private CustomerIdentify currentCustomerIdentify;
    private int countOfCombo;

    public Action<ComboData> OnMatch { get; set; }

    public void Configure(IComboManager comboManager)
    {
        _comboManager = comboManager;
    }

    public int GetReward(int comboSize)
    {
        return 0;
    }

    public ComboData RegisterServed(ComboInput input)
    {
        var customer = input.customer;

        if (currentCustomerIdentify == CustomerIdentify.None)
        {
            currentCustomerIdentify = customer.customerIdentify;
        }

        if (customer.customerIdentify == currentCustomerIdentify)
        {
            countOfCombo++;
        }
        else
        {
            countOfCombo = 1;
            currentCustomerIdentify = customer.customerIdentify;
        }

        currentComboData = new ComboData
        {
            comboSize = countOfCombo,
            comboType = GetComboType(currentCustomerIdentify),
            matched = false
        };

        if (countOfCombo >= 3)
        {
            currentComboData.matched = true;

            OnMatch?.Invoke(currentComboData);

            countOfCombo = 0;
            currentCustomerIdentify = CustomerIdentify.None;
        }

        return currentComboData;
    }

    private ComboType GetComboType(CustomerIdentify customerIdentify)
    {
        return customerIdentify switch
        {
            CustomerIdentify.Casual => ComboType.Casual,
            CustomerIdentify.Apurado => ComboType.Rush,
            CustomerIdentify.VIP => ComboType.Vip,
            _ => ComboType.None
        };
    }

    public ComboData GetComboData()
    {
        return currentComboData;
    }
}