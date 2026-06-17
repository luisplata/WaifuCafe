using System;
using UnityEngine;

public class CustomerComboManager : MonoBehaviour, ICustomComboManager
{
    private IComboManager _comboManager;
    private ComboData currentComboData;
    private CustomerIdentify currentCustomerIdentify;
    private int countOfCombo;

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
            comboType = GetComboType(currentCustomerIdentify)
        };

        if (countOfCombo >= 3)
        {
            OnMatch?.Invoke(currentComboData);
            // Match!
            countOfCombo = 0;
            currentCustomerIdentify = CustomerIdentify.None;

            currentComboData = new ComboData
            {
                comboSize = countOfCombo,
                comboType = GetComboType(currentCustomerIdentify)
            };
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
            _ => ComboType.CustomerMatch
        };
    }

    public ComboData GetComboData()
    {
        return currentComboData;
    }

    public Action<ComboData> OnMatch { get; set; }
}